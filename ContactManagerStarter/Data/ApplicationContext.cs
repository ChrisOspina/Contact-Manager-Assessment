using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContactManager.Data
{
    public class ApplicationContext : DbContext
    {
        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<EmailAddress> EmailAddresses { get; set; }
        public virtual DbSet<Address> Addresses { get; set; }
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicationContext> _logger;


        public ApplicationContext(DbContextOptions<ApplicationContext> options, IConfiguration configuration,  ILogger<ApplicationContext> logger)
            : base(options)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        }
    }
}
