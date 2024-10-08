﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TravelRecordsAPI.Models;

namespace TravelRecordsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly CoreDbContext _context;

        public PostsController(CoreDbContext context)
        {
            _context = context;
        }

        [HttpGet, Authorize]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts(int stageId)
        {
            return await _context.Posts.ToListAsync();
        }

        [HttpGet("{stageId}/stagePosts"), Authorize]
        public async Task<ActionResult<IEnumerable<Post>>> GetStagePosts(int stageId)
        {
            var stage = await _context.Stages.FindAsync(stageId);
            if (stage == null)
            {
                return NotFound();
            }

            List<Post> stagePosts = _context.Posts.Where(x => x.StageId==stageId).ToList();
            return stagePosts;
        }

        [HttpGet("{tripId}/tripPosts"), Authorize]
        public async Task<ActionResult<IEnumerable<Post>>> GetTripPosts(int tripId)
        {
            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null)
            {
                return NotFound();
            }

            List<Post> stagePosts = _context.Posts.Where(x => x.TripId == tripId).ToList();
            return stagePosts;
        }

        [HttpGet("{id}"), Authorize]
        public async Task<ActionResult<Post>> GetPost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            return post;
        }

        [HttpPost,Authorize]
        public async Task<ActionResult<Post>> PostPost(Post post)
        {
            var stage = await _context.Stages.FindAsync(post.StageId);
            var trip = await _context.Trips.FindAsync(post.TripId);
            var user = await _context.Users.FindAsync(post.UserId);
            if (stage == null||trip==null||user==null)
            {
                return BadRequest();
            }
            if (string.IsNullOrEmpty(post.Story) || post.Story == "null")
            {
                return BadRequest("invalid story value");
            }
            post.PostId = this.GetStageId();
            post.CreationDate = DateTime.Now;
            _context.Posts.Add(post);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (this.PostExists(post.TripId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetPost", new { id = post.TripId }, post);
        }

        [HttpPut("{id}"), Authorize]
        public async Task<IActionResult> PutPost(int id, Post post)
        {
            if (id != post.PostId)
            {
                return BadRequest();
            }

            var stage = await _context.Stages.FindAsync(post.StageId);
            var trip = await _context.Trips.FindAsync(post.TripId);
            var user = await _context.Users.FindAsync(post.UserId);
            if (stage == null || trip == null || user == null)
            {
                return BadRequest();
            }
            if (string.IsNullOrEmpty(post.Story) || post.Story == "null")
            {
                return BadRequest("invalid story value");
            }

            _context.Entry(post).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(id))
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

        [HttpDelete("{id}"), Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }

        private int GetStageId()
        {
            if (_context.Posts.Count() == 0)
            {
                return 1;
            }
            int maxPostId = _context.Posts.Max(x => x.PostId);
            maxPostId++;
            return maxPostId;
        }
    }
}
