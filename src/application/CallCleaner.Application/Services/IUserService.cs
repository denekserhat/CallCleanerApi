using AutoMapper;
using AutoMapper.QueryableExtensions;
using CallCleaner.Application.Dtos.Core;
using CallCleaner.Application.Dtos.User;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CallCleaner.Application.Services;

public interface IUserService
{
    Task<(List<UserDTO> Users, PaginationMetaData Pagination)> GetAllUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<UserDTO> GetUserByIdAsync(int id, CancellationToken cancellationToken);
    Task<UserDTO> CreateUserAsync(CreateUserDTO createUserDTO, CancellationToken cancellationToken);
    Task<UserDTO> UpdateUserAsync(int id, UpdateUserDTO updateUserDTO, CancellationToken cancellationToken);
    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken);
    Task<AppUser> GetUserForUpdateAsync(int userId, CancellationToken cancellationToken);
    Task<bool> CheckUserExistsAsync(int userId, CancellationToken cancellationToken);

}

public class UserService : IUserService
{
    private const int CACHE_DURATION_MINUTES = 5;
    private const string USER_CACHE_KEY_PREFIX = "user_";
    private const string USERS_PAGE_CACHE_PREFIX = "users_page_";

    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPasswordService _passwordService;

    public UserService(
        DataContext context,
        IMapper mapper,
        ILogger<UserService> logger,
        ICacheService cacheService,
        IPasswordService passwordService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }

    public async Task<(List<UserDTO> Users, PaginationMetaData Pagination)> GetAllUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"{USERS_PAGE_CACHE_PREFIX}{pageNumber}_{pageSize}";
            var cachedResult = await _cacheService.GetAsync<(List<UserDTO>, PaginationMetaData)>(cacheKey);

            if (cachedResult != default)
            {
                return cachedResult;
            }

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var paginationMetaData = new PaginationMetaData(totalCount, pageSize, pageNumber);

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<UserDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = (users, paginationMetaData);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching users. PageNumber: {PageNumber}, PageSize: {PageSize}",
                pageNumber, pageSize);
            throw new ApplicationException("Failed to retrieve users", ex);
        }
    }
    public async Task<UserDTO> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"{USER_CACHE_KEY_PREFIX}{id}";
            var cachedUser = await _cacheService.GetAsync<UserDTO>(cacheKey);

            if (cachedUser != null)
            {
                return cachedUser;
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            var userDto = _mapper.Map<UserDTO>(user);
            await _cacheService.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with ID: {UserId}", id);
            throw new ApplicationException($"Failed to retrieve user with ID {id}", ex);
        }
    }
    public async Task<UserDTO> CreateUserAsync(CreateUserDTO createUserDTO, CancellationToken cancellationToken)
    {
        try
        {
            if (await _context.Users.AnyAsync(u =>
                u.UserName == createUserDTO.Username ||
                u.Email == createUserDTO.Email,
                cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Username or email already exists");
            }

            var user = _mapper.Map<AppUser>(createUserDTO);
            user.PasswordHash = _passwordService.HashPassword(createUserDTO.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _cacheService.RemoveByPrefixAsync(USERS_PAGE_CACHE_PREFIX);
            return _mapper.Map<UserDTO>(user);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user");
            throw new ApplicationException("Failed to create user", ex);
        }
    }
    public async Task<UserDTO> UpdateUserAsync(int id, UpdateUserDTO updateUserDTO, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {id} not found");
            }

            if (updateUserDTO.Username != null &&
                updateUserDTO.Username != user.UserName &&
                await _context.Users.AnyAsync(u =>
                    u.UserName == updateUserDTO.Username &&
                    u.Id != id,
                    cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Username is already taken");
            }

            _mapper.Map(updateUserDTO, user);
            user.UpdateDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _cacheService.RemoveAsync($"{USER_CACHE_KEY_PREFIX}{id}");
            await _cacheService.RemoveByPrefixAsync(USERS_PAGE_CACHE_PREFIX);

            return _mapper.Map<UserDTO>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {UserId}", id);
            throw new ApplicationException($"Failed to update user with ID {id}", ex);
        }
    }
    public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _cacheService.RemoveAsync($"{USER_CACHE_KEY_PREFIX}{id}");
            await _cacheService.RemoveByPrefixAsync(USERS_PAGE_CACHE_PREFIX);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}", id);
            throw new ApplicationException($"Failed to delete user with ID {id}", ex);
        }
    }
    public async Task<bool> CheckUserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }
    public async Task<AppUser> GetUserForUpdateAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        return user;
    }
}
