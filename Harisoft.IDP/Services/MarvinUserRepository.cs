using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Harisoft.IDP.Entities;
using Microsoft.EntityFrameworkCore;

namespace Harisoft.IDP.Services
{
    public class MarvinUserRepository : IMarvinUserRepository
    {
        MarvinUserContext _context;

        public MarvinUserRepository(MarvinUserContext context)
        {
            _context = context;
        }

        public bool AreUserCredentialsValid(string username, string password)
        {
            // get the user
            var user = GetUserByUsername(username);
            if (user == null)
            {
                return false;
            }

            return (user.Password == password && !string.IsNullOrWhiteSpace(password));
        }

        public User GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Claims.Any(c => c.ClaimType == "email" && c.ClaimValue == email));
        }

        public User GetUserByProvider(string loginProvider, string providerKey)
        {
            return _context.Users
                .FirstOrDefault(u => 
                    u.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey));
        }

        public User GetUserBySubjectId(string subjectId)
        {
            return _context.Users.FirstOrDefault(u => u.SubjectId == subjectId);
        }

        public User GetUserByUsername(string username)
        {
            return _context.Users.FirstOrDefault(u => u.Username == username);
        }

        public IEnumerable<UserClaim> GetUserClaimsBySubjectId(string subjectId)
        {
            // get user with claims
            var user = _context.Users.Include("Claims").FirstOrDefault(u => u.SubjectId == subjectId);
            if (user == null)
            {
                return new List<UserClaim>();
            }
            return user.Claims.ToList();
        }

        public IEnumerable<UserLogin> GetUserLoginsBySubjectId(string subjectId)
        {
            var user = _context.Users.Include("Logins").FirstOrDefault(u => u.SubjectId == subjectId);
            if (user == null)
            {
                return new List<UserLogin>();
            }
            return user.Logins.ToList();
        }

        public bool IsUserActive(string subjectId)
        {
            var user = GetUserBySubjectId(subjectId);
            return user.IsActive;
         }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
        }

        public void AddUserLogin(string subjectId, string loginProvider, string providerKey)
        {
            var user = GetUserBySubjectId(subjectId);
            if (user == null)
            {
                throw new ArgumentException("User with given subjectId not found.", subjectId);
            }

            user.Logins.Add(new UserLogin()
            {
                SubjectId = subjectId,
                LoginProvider = loginProvider,
                ProviderKey = providerKey
            });
        }

        public void AddUserClaim(string subjectId, string claimType, string claimValue)
        {          
            var user = GetUserBySubjectId(subjectId);
            if (user == null)
            {
                throw new ArgumentException("User with given subjectId not found.", subjectId);
            }

            user.Claims.Add(new UserClaim(claimType, claimValue));         
        }

        public bool Save()
        {
            return (_context.SaveChanges() >= 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
        }

       
    }
}
