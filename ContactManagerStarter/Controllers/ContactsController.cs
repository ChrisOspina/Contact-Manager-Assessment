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


//TODO: Impelement Try/Catch blocks
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

        //handling delete contact
        public async Task<IActionResult> DeleteContact(Guid id)
        {
            _logger.LogInformation("Deleting contact");
            var contactToDelete = await _context.Contacts
                .Include(x => x.EmailAddresses)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (contactToDelete == null)
            {
                _logger.LogError("There are no contacts to delete");
                return BadRequest();
            }

            _context.EmailAddresses.RemoveRange(contactToDelete.EmailAddresses);
            _context.Contacts.Remove(contactToDelete);

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("Update");

            _logger.LogInformation("Contact removed");
            return Ok();
           
        }

  
        public async Task<IActionResult> EditContact(Guid id)
        {
            var contact = await _context.Contacts
                .Include(x => x.EmailAddresses)
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (contact == null)
            {
                _logger.LogError("Contact not found");
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
            _logger.LogInformation("Contact information updated");
            return PartialView("_EditContact", viewModel);
        }

        public async Task<IActionResult> GetContacts()
        {
            var contactList = await _context.Contacts
                .OrderBy(x => x.FirstName)
                .ToListAsync();

            return PartialView("_ContactTable", new ContactViewModel { Contacts = contactList });
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
            _logger.LogInformation("Saving contact");
            var contact = model.ContactId == Guid.Empty
                ? new Contact { Title = model.Title, FirstName = model.FirstName, LastName = model.LastName, DOB = model.DOB }
                : await _context.Contacts.Include(x => x.EmailAddresses).Include(x => x.Addresses).FirstOrDefaultAsync(x => x.Id == model.ContactId);

            if (contact == null)
            {
                _logger.LogError("Contact not found");
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

            _logger.LogInformation("Contact information saved");

            return Ok();
        }
    }

}