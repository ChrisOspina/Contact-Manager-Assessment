using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContactManager.Data;
using ContactManager.Hubs;
using ContactManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MailKit;
using MimeKit;
using MailKit.Net.Smtp;
using System.Data.Common;



namespace ContactManager.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IHubContext<ContactHub> _hubContext;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(ApplicationContext context, IHubContext<ContactHub> hubContext, ILogger <ContactsController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IActionResult> DeleteContact(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting contact");
                var contactToDelete = await _context.Contacts
                    .Include(x => x.EmailAddresses)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (contactToDelete == null)
                {
                    //giving a warning message if there are no contacts
                    _logger.LogWarning("There are no contacts to delete");
                    return BadRequest();
                }

                _context.EmailAddresses.RemoveRange(contactToDelete.EmailAddresses);
                _context.Contacts.Remove(contactToDelete);

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("Update");

                _logger.LogInformation("Contact removed");
                return Ok();
            }
            //If a database update error were to occur
            catch (DbUpdateException dbex)
            {
                _logger.LogError(dbex, "Database update error while deleting contact with id {ContactId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the database" });

            }
            //For any other error in general
            catch (Exception ex) {

                _logger.LogError(ex, "An error while deleting contact with id {ContactId}", id);
                return StatusCode(500,new {Message = "An internal server error occured"});
            }
           
        }

  
        public async Task<IActionResult> EditContact(Guid id)
        {
            try
            {
                var contact = await _context.Contacts
                                .Include(x => x.EmailAddresses)
                                .Include(x => x.Addresses)
                                .FirstOrDefaultAsync(x => x.Id == id);

                if (contact == null)
                {
                    _logger.LogWarning("Contact not found");
                    return NotFound();
                }

                var viewModel = new EditContactViewModel
                {
                    Id = contact.Id,
                    Title = contact.Title,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    DOB = contact.DOB,
                    EmailAddresses = contact.EmailAddresses,
                    Addresses = contact.Addresses
                };
                _logger.LogInformation("Contact with id {ContactId} loaded succesfully for editing",id);
                return PartialView("_EditContact", viewModel);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "An error occured while loading contact id {ContactId} for editing", id);
                return StatusCode(500, new { Message = "An internal service error occured" }); 
            }
            
        }

        public async Task<IActionResult> GetContacts()
        {
            try
            {
                var contactList = await _context.Contacts
            .OrderBy(x => x.FirstName)
            .ToListAsync();

                return PartialView("_ContactTable", new ContactViewModel { Contacts = contactList });
            }

            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured while fetching contacts");
                return StatusCode(500, new { ErrorMessage = ex.Message });
            }
        
        }

        public IActionResult Index()
        {
                return View();
        }

        public IActionResult NewContact()
        {
            return PartialView("_EditContact", new EditContactViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> SaveContact([FromBody]SaveContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model for saving contact");
                return BadRequest(ModelState);
            }
            try
            {
                _logger.LogInformation("Saving contact");
                var contact = model.ContactId == Guid.Empty
                    ? new Contact { Title = model.Title, FirstName = model.FirstName, LastName = model.LastName, DOB = model.DOB }
                    : await _context.Contacts.Include(x => x.EmailAddresses).Include(x => x.Addresses).FirstOrDefaultAsync(x => x.Id == model.ContactId);

                if (contact == null)
                {
                    _logger.LogWarning("Contact with id {Contact.Id} not found",model.ContactId);
                    return NotFound();
                }

                _context.EmailAddresses.RemoveRange(contact.EmailAddresses);
                _context.Addresses.RemoveRange(contact.Addresses);

                foreach (var email in model.Emails)
                {
                    _logger.LogInformation("Saving the email address ${email.Email} into contact", email.Email);
                    contact.EmailAddresses.Add(new EmailAddress
                    {
                        Type = email.Type,
                        Email = email.Email,
                        Contact = contact
                    });
                    _logger.LogInformation("Added email {Email} to contact", email.Email);
                }

                foreach (var address in model.Addresses)
                {
                    //_logger.LogInformation("Sav")
                    contact.Addresses.Add(new Address
                    {
                        Street1 = address.Street1,
                        Street2 = address.Street2,
                        City = address.City,
                        State = address.State,
                        Zip = address.Zip,
                        Type = address.Type
                    });
                    _logger.LogInformation("Added address {Street1}, {City} to contact", address.Street1, address.City);
                }
                

                _logger.LogInformation("Saving title into contact");
                contact.Title = model.Title;

                _logger.LogInformation("Saving first and last name into contact");
                contact.FirstName = model.FirstName;
                contact.LastName = model.LastName;

                _logger.LogInformation("Saving Date of Birth into contact");
                contact.DOB = model.DOB;

                if (model.ContactId == Guid.Empty)
                {
                    _logger.LogInformation($"ContactId: {model.ContactId} added");
                    await _context.Contacts.AddAsync(contact);
                }
                else
                {
                    _logger.LogInformation($"ContactId: {model.ContactId} updated");
                    _context.Contacts.Update(contact);
                }


                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("Update");

                _logger.LogInformation("Contact information saved succesfully");

                return Ok();
            }
            catch (DbUpdateException dbex) {
                _logger.LogError(dbex, "Database update error while saving contact with ID {ContactId}", model.ContactId);
                return StatusCode(500, new { Message = "An error occurred while updating the database" });

            }
            catch (Exception ex){
                _logger.LogError(ex, "An error occurred while saving contact with ID {ContactId}", model.ContactId);
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }

            

          
        }
    }

}