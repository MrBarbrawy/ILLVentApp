using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ILLVentApp.Infrastructure.Services
{
    public class CustomUserManager : UserManager<User>
    {
        private readonly IUserFriendlyIdService _userFriendlyIdService;

        public CustomUserManager(
            IUserStore<User> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<User>> logger,
            IUserFriendlyIdService userFriendlyIdService)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _userFriendlyIdService = userFriendlyIdService;
        }

        public override async Task<IdentityResult> CreateAsync(User user)
        {
            // Generate a user-friendly ID
            user.Id = _userFriendlyIdService.GenerateUserId();
            return await base.CreateAsync(user);
        }

        public override async Task<IdentityResult> CreateAsync(User user, string password)
        {
            // Generate a user-friendly ID
            user.Id = _userFriendlyIdService.GenerateUserId();
            return await base.CreateAsync(user, password);
        }
    }
} 