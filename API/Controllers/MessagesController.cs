using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWOrk _unitOfWOrk;
        
        private readonly IMapper _mapper;
        public MessagesController(IMapper mapper, IUnitOfWOrk unitOfWOrk)
        {
            _unitOfWOrk = unitOfWOrk;
            _mapper = mapper;
            
        }
        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto){
            var username = User.GetUsername();
            if(username == createMessageDto.RecipientUsername.ToLower())
                return BadRequest("You cannot send messages to yourself");
            var sender = await _unitOfWOrk.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unitOfWOrk.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);
            if(recipient == null) return NotFound();
            var message = new Message{
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };
            _unitOfWOrk.MessageRepository.AddMessage(message);
            if(await _unitOfWOrk.Complete()) return Ok(_mapper.Map<MessageDto>(message));
            return BadRequest("Failed to send message");
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery]
            MessageParams messageParams){
            messageParams.Username = User.GetUsername();
            var messages = await _unitOfWOrk.MessageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                messages.TotalCount, messages.TotalPages);
            return messages;
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id){
            var username = User.GetUsername();
            var message = await _unitOfWOrk.MessageRepository.GetMessage(id);
            if(message.Sender.UserName != username && message.Recipient.UserName != username)
                return Unauthorized();
            if(message.Sender.UserName == username)
                message.SenderDeleted = true;
            if(message.Recipient.UserName == username)
                message.RecipientDeleted = true;
            if(message.SenderDeleted && message.RecipientDeleted)
                _unitOfWOrk.MessageRepository.DeleteMessage(message);
            if(await _unitOfWOrk.Complete()) return Ok();
            return BadRequest("Problem deleting the message");
        }
    }
}