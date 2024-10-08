﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TravelRecordsAPI.Models;
using TravelRecordsAPI.Models.Dto;

namespace TravelRecordsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly CoreDbContext _context;

        public UsersController(CoreDbContext context)
        {
            _context = context;
        }

        //GET: api/Users
        [HttpGet, AllowAnonymous]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}"), AllowAnonymous]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpGet("{username}/{password}"), AllowAnonymous]
        public async Task<ActionResult<User>> GetUser(string username, string password)
        {
            if (await _context.Users.AnyAsync(e => e.Username == username)) 
            {
                PasswordConverter passConv = new PasswordConverter(password);
                password = passConv.GetHashedPassword();

                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username && e.Password == password); 

                if (user != null)
                {
                    return user;
                }

                // Wrong password
                return StatusCode(403);
            }
            else
            {
                return NotFound(); // 404
            }
        }

        [HttpPut("{id}"),Authorize]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(user.Username) || user.Username == "null")
            {
                return BadRequest("invalid username");
            }
            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("invalid password");
            }
            if (string.IsNullOrEmpty(user.Email) || user.Email == "null")
            {
                return BadRequest("invalid username");
            }

            if (ChangedUsername(user.UserId,user.Username))
            {
                if (UsernameExists(user.Username))
                {
                    return Conflict();
                }
            }

            if(ChangedEmail(user.UserId,user.Email))
            {
                if(EmailExists(user.Email))
                {
                    return Conflict();
                }
            }

            PasswordConverter passCov = new PasswordConverter(user.Password);
            user.Password = passCov.GetHashedPassword();

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if(_context.Users.Count()==0)
            {
                user.UserId =1;
            }
            else
            {
                int maxUserId = _context.Users.Max(x => x.UserId);
                user.UserId = maxUserId + 1;
            }          
            if(string.IsNullOrEmpty(user.Username) ||user.Username=="null")
            {
                return BadRequest("invalid username");
            }
            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("invalid password");
            }
            if (string.IsNullOrEmpty(user.Email) || user.Email == "null")
            {
                return BadRequest("invalid username");
            }
            PasswordConverter passCov = new PasswordConverter(user.Password);
            user.Password = passCov.GetHashedPassword();

            _context.Users.Add(user);
            //username must be unique
            if(UsernameExists(user.Username))
            {
                return Conflict();
            }
            if(EmailExists(user.Email))
            {
                return Conflict();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserExists(user.UserId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        [HttpDelete("{id}"),Authorize]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        private bool UsernameExists(string username)
        {
            return _context.Users.Any(e => e.Username == username);
        }

        private bool EmailExists(string email)
        {
            return _context.Users.Any(e => e.Email == email);
        }

        private bool ChangedUsername(int id,string username)
        {
            if (UserExists(id))
            {
                return _context.Users.Any(e => (e.UserId == id) && (e.Username != username));
            }
            return false;
        }

        private bool ChangedEmail(int id, string email)
        {
            if (UserExists(id))
            {
                return _context.Users.Any(e => (e.UserId == id) && (e.Email != email));
            }
            return false;
        }
    }
}
