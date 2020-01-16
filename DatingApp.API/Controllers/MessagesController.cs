using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers {
    [ServiceFilter (typeof (LogUserActivity))]
    [Authorize]
    [Route ("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase {
        private IDatingRepository _repo;
        private IMapper _mapper;

        public MessagesController (IDatingRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet ("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage (int userId, int id) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            var messageFromRepo = await _repo.GetMessage (id);
            if (messageFromRepo == null) {
                return NotFound ();
            }

            return Ok (messageFromRepo);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage (int userId, MessageForCreationDto messageForCreationDto) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            messageForCreationDto.SenderId = userId;

            var recipient = await _repo.GetUser (messageForCreationDto.RecipientId);

            if (recipient == null) {
                return BadRequest ("Cannot find recipient");
            }

            var message = _mapper.Map<Message> (messageForCreationDto);
            _repo.Add (message);

            if (await _repo.SaveAll ()) {
                var messageToReturn = _mapper.Map<MessageForCreationDto> (message);
                return CreatedAtRoute ("GetMessage", new { userId = userId, id = message.Id }, messageToReturn);
            }

            throw new Exception ("Creating the message failed on save");
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser (int userId, [FromQuery] MessageParams messageParams) {

            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);

            var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            Response.AddPagination(messageParams.PageNumber, messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalCount);

            return Ok(messages);
        }

    }
}