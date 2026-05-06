using Microsoft.EntityFrameworkCore;
using NKZAPI.Models;
using NKZAPI.Services.PassService;

namespace NKZAPI.Data
{
    public static class AdminSeed
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var email = configuration["AdminSeed:Email"] ?? configuration["ADMIN_EMAIL"];
            var password = configuration["AdminSeed:Password"] ?? configuration["ADMIN_PASSWORD"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NKZAPIContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordInterface>();

            await SeedPlansAsync(context);

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await context.Users.FirstOrDefaultAsync(item => item.Email.ToLower() == normalizedEmail);
            var resetPassword = string.Equals(configuration["AdminSeed:ResetPassword"], "true", StringComparison.OrdinalIgnoreCase);

            if (user == null)
            {
                passwordService.CreatePassHash(password, out var hash, out var salt);
                user = new User
                {
                    Email = normalizedEmail,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = "Admin",
                    Player = new List<Player>(),
                    EmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    DiscordVerified = true,
                    DiscordUsername = configuration["AdminSeed:DiscordUsername"] ?? "admin",
                    DiscordVerifiedAt = DateTime.UtcNow
                };

                context.Users.Add(user);
            }
            else
            {
                user.Role = "Admin";
                user.EmailVerified = true;
                user.EmailVerifiedAt ??= DateTime.UtcNow;
                user.DiscordVerified = true;
                user.DiscordUsername ??= configuration["AdminSeed:DiscordUsername"] ?? "admin";
                user.DiscordVerifiedAt ??= DateTime.UtcNow;

                if (resetPassword)
                {
                    passwordService.CreatePassHash(password, out var hash, out var salt);
                    user.PasswordHash = hash;
                    user.PasswordSalt = salt;
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedPlansAsync(NKZAPIContext context)
        {
            if (await context.SubscriptionPlans.AnyAsync()) return;

            context.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Name = "Starter",
                    Description = "Plano de entrada para organizar seu perfil competitivo.",
                    Price = 0,
                    DurationMonths = 1,
                    Benefits = "Perfil competitivo ativo\nAcesso a ligas gratuitas\nHistorico basico",
                    IsActive = true,
                    SortOrder = 1
                },
                new SubscriptionPlan
                {
                    Name = "Pro Player",
                    Description = "Mais destaque para jogadores e capitaes que querem montar elenco.",
                    Price = 19.90m,
                    DurationMonths = 1,
                    Benefits = "Destaque em recrutamento\nPreferencia em novidades\nHistorico competitivo ampliado",
                    IsActive = true,
                    IsFeatured = true,
                    SortOrder = 2
                },
                new SubscriptionPlan
                {
                    Name = "Captain",
                    Description = "Ferramentas futuras para capitaes, convites e gestao de time.",
                    Price = 49.90m,
                    DurationMonths = 1,
                    Benefits = "Recursos de capitao\nVisao de lacunas do time\nPrioridade em suporte competitivo",
                    IsActive = true,
                    SortOrder = 3
                });

            await context.SaveChangesAsync();
        }
    }
}
