using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IUserService : IServiceBase<User>
    {
        Task<LoginResult> Authenticate(string username, string password, string captcharToken, int? totp, bool ignoreCaptchar = false);
        Task<User> GetUser(int id);
        Task<bool> CheckToken(Guid token);
        Task<RegisterResponse> CreateUser(RegisterRequest registerRequest);
        Task<List<User>> SearchUser(string username);
        Task<ApiResponseBaseModel<User>> CreateStaff(RegisterNoCaptcharRequest registerRequest);

        Task<GenerateApiKeyResponse> GenerateNewApiKey(int userId);
        Task<bool> IsBanned(Guid token);
        Task<int> GetUserIdFromApiKey(string apiKey);
        Task<ApiResponseBaseModel<User>> AssignGsmsToUser(int userId, AssignGsmToUserRequest request);
        Task<User> CheckUser(string username);
        Task<ApiResponseBaseModel> ToggleReferEnabled(int userId, bool enabled);
        Task ProcessUserTokens();
        Task<User> GetCurrentUser();
        Task SnapshotBalance();
        void ClearLoginCache(User user);
    }
    public class UserService : ServiceBase<User>, IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly IDateTimeService _dateTimeService;
        private readonly IAuthService _authService;
        private readonly IRecaptcharService _recaptcharService;
        private readonly IMemoryCache _memoryCache;
        private readonly string USER_TOKEN_CACHE_KEY_FORMAT = "USER_TOKEN_CACHE_KEY_FORMAT_{0}";
        private readonly string USER_ID_FROM_API_KEY_CACHE_KEY_FORMAT = "USER_ID_FROM_API_KEY_CACHE_KEY_FORMAT_{0}";
        private readonly ILogger _logger;

        private DbSet<User> _users
        {
            get
            {
                return _smsDataContext.Users;
            }
        }

        private DbSet<UserProfile> _userProfiles
        {
            get
            {
                return _smsDataContext.UserProfiles;
            }
        }

        private DbSet<UserToken> _userTokens
        {
            get
            {
                return _smsDataContext.UserTokens;
            }
        }

        public UserService(IOptions<AppSettings> appSettings,
            SmsDataContext smsDataContext,
            IDateTimeService dateTimeService, IAuthService authService, IRecaptcharService recaptcharService,
            IMemoryCache memoryCache, ILogger<UserService> logger) : base(smsDataContext)
        {
            _appSettings = appSettings.Value;
            _dateTimeService = dateTimeService;
            _authService = authService;
            _recaptcharService = recaptcharService;
            _memoryCache = memoryCache;
            _logger = logger;
        }
        public async Task<LoginResult> Authenticate(string username, string password, string captcharToken, int? totp, bool ignoreCaptchar = false)
        {
            //if (!ignoreCaptchar && !_recaptcharService.Validate(captcharToken))
            //{
            //    return new LoginResult()
            //    {
            //        Success = false,
            //        Message = "CaptcharFailed"
            //    };
            //}
            var user = await _users.SingleOrDefaultAsync(x => x.Username == username && x.Password == password);

            // return null if user not found
            if (user == null)
                return new LoginResult() { Success = false, Message = "WrongUsernameOrPassword" };

            // check user has role admin
            if (user.Role == RoleType.Administrator)
            {
                //var toptValidator = new AspNetCore.Totp.TotpValidator(new AspNetCore.Totp.TotpGenerator());
                //var toptResult = toptValidator.Validate($"{user.Username}X{user.Guid}", totp.GetValueOrDefault());
                //if (!toptResult)
                //{
                //    return new LoginResult() { Success = false, Message = "Wrong2FACode" };
                //}
            }

            var userToken = new UserToken()
            {
                Expired = _dateTimeService.UtcNow().AddDays(7),
                Token = Guid.NewGuid(),
                UserId = user.Id
            };
            _userTokens.Add(userToken);
            await _smsDataContext.SaveChangesAsync();

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Hash, userToken.Token.ToString()),
                    new Claim(ClaimTypes.Role, user.Role == RoleType.Administrator ? "Administrator" : user.Role == RoleType.Staff ? "Staff" : user.Role == RoleType.Forwarder ? "Forwarder" : "User")
                }),
                Expires = _dateTimeService.UtcNow().AddYears(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            string strToken = tokenHandler.WriteToken(token);
            return new LoginResult()
            {
                Success = true,
                Token = strToken
            };
        }
        public async Task<bool> CheckToken(Guid token)
        {
            var userToken = await GetUserToken(token);
            if (userToken == null) return false;
            if (userToken.Expired < _dateTimeService.UtcNow()) return false;
            return true;
        }
        public async Task<bool> IsBanned(Guid token)
        {
            var userToken = await GetUserToken(token);
            if (userToken == null) return false;
            var user = await Get(userToken.UserId);
            if (user.IsBanned)
            {
                return true;
            }
            return false;
        }
        private async Task<UserToken> GetUserToken(Guid token)
        {
            return await _memoryCache.GetOrCreateAsync(string.Format(USER_TOKEN_CACHE_KEY_FORMAT, token), async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromHours(2));
                var entity = await _userTokens.FirstOrDefaultAsync(x => x.Token == token);
                if (entity == null)
                {
                    return null;
                }

                entity.Expired = _dateTimeService.UtcNow().AddDays(7);
                await _smsDataContext.SaveChangesAsync();
                return new UserToken()
                {
                    Expired = entity.Expired,
                    UserId = entity.UserId
                };
            });
        }
        public async Task<User> GetUser(int id)
        {
            var user = await _users.FindAsync(id);
            return user;
        }

        public async Task<RegisterResponse> CreateUser(RegisterRequest registerRequest)
        {
            if (!_recaptcharService.Validate(registerRequest.Captchar))
            {
                return new RegisterResponse()
                {
                    Success = false,
                    Message = "CaptcharFailed"
                };
            }
            if (await _users.AnyAsync(x => x.Username == registerRequest.Username))
            {
                return new RegisterResponse()
                {
                    Success = false,
                    Message = "DuplicateUsername"
                };
            }

            if (!string.IsNullOrEmpty(registerRequest.Email) && await _userProfiles.AnyAsync(x => x.Email == registerRequest.Email))
            {
                return new RegisterResponse()
                {
                    Success = false,
                    Message = "DuplicateEmail"
                };
            }
            var referalId = 0;
            if (!string.IsNullOrEmpty(registerRequest.ReferalCode))
            {
                var referalCode = registerRequest.ReferalCode.ToUpper();
                referalId = await _smsDataContext.Users.Where(r => r.Username.ToUpper() == referalCode && r.ReferEnabled == true)
                    .Select(r => r.Id).FirstOrDefaultAsync();
                if (referalId == 0) return new RegisterResponse()
                {
                    Success = false,
                    Message = "ReferalNotFound"
                };
            }

            var user = new User()
            {
                ApiKey = Helpers.Helpers.GenerateApiKey(),
                Password = registerRequest.Password,
                Role = RoleType.User,
                Username = registerRequest.Username,
                UserProfile = new UserProfile()
                {
                    Email = registerRequest.Email,
                    PhoneNumber = registerRequest.PhoneNumber,
                    Name = registerRequest.Name
                }
            };
            if (referalId != 0)
            {
                user.ReferalId = referalId;
            }
            _users.Add(user);
            await _smsDataContext.SaveChangesAsync();
            return new RegisterResponse()
            {
                Success = true
            };
        }
        public async Task<ApiResponseBaseModel<User>> CreateStaff(RegisterNoCaptcharRequest registerRequest)
        {
            if (await _users.AnyAsync(x => x.Username == registerRequest.Username))
            {
                return new ApiResponseBaseModel<User>()
                {
                    Success = false,
                    Message = "DuplicateUsername"
                };
            }

            if (!string.IsNullOrEmpty(registerRequest.Email) && await _userProfiles.AnyAsync(x => x.Email == registerRequest.Email))
            {
                return new ApiResponseBaseModel<User>()
                {
                    Success = false,
                    Message = "DuplicateEmail"
                };
            }

            var user = new User()
            {
                ApiKey = Helpers.Helpers.GenerateApiKey(),
                Password = registerRequest.Password,
                Role = RoleType.Staff,
                Username = registerRequest.Username,
                UserProfile = new UserProfile()
                {
                    Email = registerRequest.Email,
                    PhoneNumber = registerRequest.PhoneNumber,
                    Name = registerRequest.Name
                }
            };
            _users.Add(user);
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<User>()
            {
                Success = true,
                Results = user
            };
        }

        public async Task<GenerateApiKeyResponse> GenerateNewApiKey(int userId)
        {
            var user = await _users.FindAsync(userId);
            if (user == null)
            {
                return new GenerateApiKeyResponse()
                {
                    Message = "NotFound",
                    Success = false
                };
            }
            _memoryCache.Remove(string.Format(USER_ID_FROM_API_KEY_CACHE_KEY_FORMAT, user.ApiKey));
            user.ApiKey = Helpers.Helpers.GenerateApiKey();
            await _smsDataContext.SaveChangesAsync();
            return new GenerateApiKeyResponse()
            {
                ApiKey = user.ApiKey,
                Success = true
            };
        }

        public async Task<int> GetUserIdFromApiKey(string apiKey)
        {
            var userId = await _memoryCache.GetOrCreateAsync(string.Format(USER_ID_FROM_API_KEY_CACHE_KEY_FORMAT, apiKey), async settings =>
             {
                 settings.SetAbsoluteExpiration(TimeSpan.FromHours(3));
                 return await _users.Where(x => x.ApiKey == apiKey && x.IsBanned != true).Select(r => r.Id).FirstOrDefaultAsync();
             });
            if (userId != 0)
            {
                _authService.SetCurrentUserId(userId);
            }
            return userId;
        }

        public override void Map(User entity, User model)
        {
            entity.Password = model.Password;
            entity.ApiKey = model.ApiKey;
        }
        protected override IQueryable<User> GenerateQuery(FilterRequest filterRequest)
        {
            var query = base.GenerateQuery();
            //query = query.Include(x => x.UserProfile).Include(r => r.UserGsmDevices);
            query = query.Include(x => x.UserProfile);
            if (filterRequest != null)
            {
                query = query.Where(x => x.Role != RoleType.Administrator);
                var sortColumn = (filterRequest.SortColumnName ?? string.Empty).ToLower();
                var isAsc = filterRequest.IsAsc;
                var isForStaff = false;
                var search = string.Empty;
                {
                    if (filterRequest.SearchObject.TryGetValue("SearchText", out object obj) == true)
                    {
                        search = (string)obj;
                    }
                }
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x => x.Username.Contains(search) || x.UserProfile.Name.Contains(search) || x.UserProfile.Email.Contains(search) || x.UserProfile.PhoneNumber.Contains(search));
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("forStaff", out object obj) == true)
                    {
                        isForStaff = (bool)obj;
                    }
                }
                if (isForStaff)
                {
                    query = query.Where(r => r.Role == RoleType.Staff);
                }
                else
                {
                    query = query.Where(r => r.Role == RoleType.User);
                }
                switch (sortColumn)
                {
                    case "name":
                        query = isAsc ? query.OrderBy(x => x.UserProfile.Name) : query.OrderByDescending(x => x.UserProfile.Name);
                        break;
                    case "email":
                        query = isAsc ? query.OrderBy(x => x.UserProfile.Email) : query.OrderByDescending(x => x.UserProfile.Email);
                        break;
                    case "phone":
                        query = isAsc ? query.OrderBy(x => x.UserProfile.PhoneNumber) : query.OrderByDescending(x => x.UserProfile.PhoneNumber);
                        break;
                    case "banned":
                        query = isAsc ? query.OrderBy(x => x.IsBanned) : query.OrderByDescending(x => x.IsBanned);
                        break;
                    case "ballance":
                        query = isAsc ? query.OrderBy(x => x.Ballance) : query.OrderByDescending(x => x.Ballance);
                        break;
                    case "username":
                        query = isAsc ? query.OrderBy(x => x.Username) : query.OrderByDescending(x => x.Username);
                        break;
                    default:
                        break;
                }
            }
            return query;
        }
        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<User> query)
        {
            IQueryable<UserBalanceSnapshot> queryUserBalanceByDate = _smsDataContext.UserBalanceSnapshots.OrderByDescending(r => r.Created);
            int totalBalanceOfStartDate = 0;
            int totalBalanceOfEndDate = 0;
            if (filterRequest != null)
            {
                var isForStaff = false;
                var search = string.Empty;
                {
                    if (filterRequest.SearchObject.TryGetValue("SearchText", out object obj) == true)
                    {
                        search = (string)obj;
                    }
                }
                if (!string.IsNullOrEmpty(search))
                {
                    queryUserBalanceByDate = queryUserBalanceByDate.Where(x => x.User.Username.Contains(search) || x.User.UserProfile.Name.Contains(search) || x.User.UserProfile.Email.Contains(search) || x.User.UserProfile.PhoneNumber.Contains(search));
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("forStaff", out object obj) == true)
                    {
                        isForStaff = (bool)obj;
                    }
                }
                if (isForStaff)
                {
                    queryUserBalanceByDate = queryUserBalanceByDate.Where(r => r.User.Role == RoleType.Staff);
                }
                else
                {
                    queryUserBalanceByDate = queryUserBalanceByDate.Where(r => r.User.Role == RoleType.User);
                }
                DateTime? fromDate = null;
                DateTime? toDate = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("searchStartDate", out object obj))
                    {
                        fromDate = DateTime.Parse((string)obj);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("searchEndDate", out object obj))
                    {
                        toDate = DateTime.Parse((string)obj);
                    }
                }
                if (fromDate != null)
                {
                    var year = ((DateTime)fromDate).Year;
                    var month = ((DateTime)fromDate).Month;
                    var date = ((DateTime)fromDate).Day;
                    totalBalanceOfStartDate = await queryUserBalanceByDate.Where(r => r.Year == year && r.Month == month && r.Date == date).SumAsync(r => (int)r.Balance);

                }
                if (toDate != null)
                {
                    var year = ((DateTime)toDate).Year;
                    var month = ((DateTime)toDate).Month;
                    var date = ((DateTime)toDate).Day;
                    totalBalanceOfEndDate = await queryUserBalanceByDate.Where(r => r.Year == year && r.Month == month && r.Date == date).SumAsync(r => (int)r.Balance);
                }
            }
            return new Dictionary<string, object>()
            {
                {"TotalBalance", await query.SumAsync(r=>r.Ballance) },
                {"TotalBalanceOfStartDate", totalBalanceOfStartDate },
                {"TotalBalanceOfEndDate", totalBalanceOfEndDate }
            };
        }

        public async Task<ApiResponseBaseModel<User>> AssignGsmsToUser(int userId, AssignGsmToUserRequest request)
        {
            var staff = await Get(userId);
            if (staff == null)
            {
                return new ApiResponseBaseModel<User>()
                {
                    Success = false,
                    Message = "NotFound"
                };
            }

            var allConfigs = await _smsDataContext.UserGsmDevices.Where(r => r.UserId == userId).ToListAsync();
            foreach (var config in allConfigs)
            {
                if (!request.GsmDeviceIds.Any(r => r == config.GsmDeviceId))
                {
                    _smsDataContext.UserGsmDevices.Remove(config);
                }
            }
            var notIn = request.GsmDeviceIds.Where(r => !allConfigs.Any(x => x.GsmDeviceId == r)).ToList();
            foreach (var ni in notIn)
            {
                var config = new UserGsmDevice()
                {
                    UserId = userId,
                    GsmDeviceId = ni
                };
                _smsDataContext.UserGsmDevices.Add(config);
            }

            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel<User>()
            {
                Success = true,
                Results = await Get(userId)
            };
        }

        public async Task<ApiResponseBaseModel> ToggleReferEnabled(int userId, bool enabled)
        {
            var currentUserId = _authService.CurrentUserId();
            if (currentUserId == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            var currentUser = await Get(currentUserId.Value);
            if (currentUser == null || currentUser.Role != RoleType.Administrator) return ApiResponseBaseModel.UnAuthorizedResponse();
            var user = await Get(userId);
            if (user == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            user.ReferEnabled = enabled;
            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel();
        }

        public async Task<User> GetCurrentUser()
        {
            var currentUserId = _authService.CurrentUserId();
            if (!currentUserId.HasValue) return null;
            return await Get(currentUserId.Value);
        }

        public async Task ProcessUserTokens()
        {
            var expiredUserTokens = await _smsDataContext.UserTokens.Where(r => r.Expired < _dateTimeService.UtcNow().AddHours(-2))
                .OrderBy(r => r.Id)
                .Take(50)
                .ToListAsync();
            _logger.LogInformation("Delete expired tokens: {0}", expiredUserTokens.Count);
            _smsDataContext.UserTokens.RemoveRange(expiredUserTokens);
            await _smsDataContext.SaveChangesAsync();
        }

        public async Task<List<User>> SearchUser(string username)
        {
            var currentUserId = _authService.CurrentUserId();
            username = (username ?? string.Empty).ToLower();
            var query = await _smsDataContext.Users.Include(r => r.UserProfile)
            .Where(r => r.Username.Contains(username) && r.Role == RoleType.User && r.Id != currentUserId.GetValueOrDefault())
            .OrderBy(r => r.Username)
            .Take(100)
            .ToListAsync();
            var results = query.Select(r => new User()
            {
                Id = r.Id,
                Username = r.Username,
                UserProfile = new UserProfile()
                {
                    Name = r.UserProfile?.Name,
                    Email = r.UserProfile?.Email,
                    PhoneNumber = r.UserProfile?.PhoneNumber,
                }
            }).ToList();
            return results;
        }

        public async Task<User> CheckUser(string username)
        {
            var currentUserId = _authService.CurrentUserId();
            username = (username ?? string.Empty).ToLower();
            var user = await _smsDataContext.Users.Include(r => r.UserProfile)
            .Where(r => r.Username == username && r.Role == RoleType.User && r.Id != currentUserId.GetValueOrDefault()).FirstOrDefaultAsync();
            if (user == null) return null;
            return new User()
            {
                Id = user.Id,
                Username = user.Username,
                UserProfile = new UserProfile()
                {
                    Name = user.UserProfile?.Name,
                    Email = user.UserProfile?.Email,
                    PhoneNumber = user.UserProfile?.PhoneNumber,
                }
            };
        }

        public async Task SnapshotBalance()
        {
            var userCount = await _smsDataContext.Users.CountAsync();
            var current = 0;
            var pageSize = 1000;
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month;
            var date = now.Day;
            while (current < userCount)
            {
                var users = await _smsDataContext.Users.OrderBy(r => r.Id).Skip(current).Take(pageSize)
                .Select(r => new { UserId = r.Id, Balance = r.Ballance }).ToListAsync();
                foreach (var user in users)
                {
                    _smsDataContext.UserBalanceSnapshots.Add(new UserBalanceSnapshot()
                    {
                        Balance = user.Balance,
                        UserId = user.UserId,
                        Year = year,
                        Month = month,
                        Date = date,
                    });
                }
                await _smsDataContext.SaveChangesAsync();
                current += pageSize;
            }
        }

        public void ClearLoginCache(User user)
        {
            var apiCacheKey = string.Format(USER_ID_FROM_API_KEY_CACHE_KEY_FORMAT, user.ApiKey);
            _memoryCache.Remove(apiCacheKey);
        }
    }
}
