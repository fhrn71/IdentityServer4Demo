using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Harisoft.IDP
{
    public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7",
                    Username = "Frank",
                    Password = "password",
                    Claims = new List<Claim>
                    {
                        new Claim("given_name", "Frank"),
                        new Claim("family_name", "Underwood"),
                        new Claim("address", "1, Main Road"),
                        new Claim("role", "FreeUser"),
                        new Claim("course", "English"),
                        new Claim("subscriptionlevel", "FreeUser"),
                        new Claim("country", "be")
                    }
                },
                new TestUser
                {
                    SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
                    Username = "Claire",
                    Password = "password",
                    Claims = new List<Claim>
                    {
                        new Claim("given_name", "Claire"),
                        new Claim("family_name", "Underwood"),
                        new Claim("address", "2, Big Street"),
                        new Claim("role", "PayingUser"),
                        new Claim("course", "French"),
                        new Claim("subscriptionlevel", "PayingUser"),
                        new Claim("country", "nl")
                    }
                }
            };
        }

        internal static IEnumerable<ApiResource> GetApiResources()
        {
            return new[] {
                new ApiResource("imagegalleryapi", "Image Gallery API", new[] { "role" }) {
                    ApiSecrets = new[] { new Secret("apisecret".Sha256()) } }
            };
        }

        public static List<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
               new IdentityResources.OpenId(),
               new IdentityResources.Profile(),
               new IdentityResources.Address(),
               new IdentityResource("courses", "Your course(s)", new []{"course"}),
               new IdentityResource("roles", "Your role(s)", new []{"role"}),
               new IdentityResource("subscriptionlevel", "Your subscription level", new[]{"subscriptionlevel"}),
               new IdentityResource("country", "Your country", new[]{"country" })
            };
        }

        public static List<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientName = "ImageGallery",
                    ClientId="imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AccessTokenType = AccessTokenType.Reference,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowOfflineAccess = true,
                    RedirectUris =new List<string>
                    {
                        "https://localhost:44389/signin-oidc"
                    },
                    PostLogoutRedirectUris = new[]{ "https://localhost:44389/signout-callback-oidc" },
                    AllowedScopes = new []
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address,
                        "roles",
                        "courses",
                        "imagegalleryapi",
                        "subscriptionlevel",
                        "country"
                    },
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("ItsMySecret".Sha256())
                    },
                    AlwaysIncludeUserClaimsInIdToken=true
                }
            };
        }
    }
}
