using Harisoft.IDP.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Harisoft.IDP.Controllers.UserRegistration
{
    public class UserRegistrationController:Controller
    {
        public UserRegistrationController(IMarvinUserRepository marvinUserRepository, IIdentityServerInteractionService interactionService) {
            MarvinUserRepository = marvinUserRepository;
            InteractionService = interactionService;
        }

        public IMarvinUserRepository MarvinUserRepository { get; }
        public IIdentityServerInteractionService InteractionService { get; }

        public IActionResult RegisterUser(RegistrationInputModel registrationInputModel)
        {
            return View(new RegisterUserViewModel {
                Provider = registrationInputModel.Provider,
                ProviderUserId = registrationInputModel.ProviderUserId,
                ReturnUrl = registrationInputModel.ReturnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // create user + claims
                var userToCreate = new Entities.User();
                userToCreate.Password = model.Password;
                userToCreate.Username = model.Username;
                userToCreate.IsActive = true;
                userToCreate.Claims.Add(new Entities.UserClaim("country", model.Country));
                userToCreate.Claims.Add(new Entities.UserClaim("address", model.Address));
                userToCreate.Claims.Add(new Entities.UserClaim("given_name", model.Firstname));
                userToCreate.Claims.Add(new Entities.UserClaim("family_name", model.Lastname));
                userToCreate.Claims.Add(new Entities.UserClaim("email", model.Email));
                userToCreate.Claims.Add(new Entities.UserClaim("subscriptionlevel", "FreeUser"));

                if(model.IsExternalProvider)
                {
                    userToCreate.Logins.Add(new Entities.UserLogin
                    {
                        LoginProvider = model.Provider,
                        ProviderKey = model.ProviderUserId
                    });
                }
                // add it through the repository
                MarvinUserRepository.AddUser(userToCreate);

                if (!MarvinUserRepository.Save())
                {
                    throw new Exception($"Creating a user failed.");
                }

                if (!model.IsExternalProvider)
                {
                    // log the user in
                    await HttpContext.SignInAsync(userToCreate.SubjectId, userToCreate.Username);
                }
                // continue with the flow     
                if (InteractionService.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return Redirect("~/");
            }

            // ModelState invalid, return the view with the passed-in model
            // so changes can be made
            return View(model);
        }
    }
}
