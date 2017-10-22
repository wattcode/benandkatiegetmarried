﻿using benandkatiegetmarried.Common.ErrorHandling;
using benandkatiegetmarried.Common.ModuleExtensions;
using benandkatiegetmarried.Common.Validation;
using benandkatiegetmarried.DAL.GuestMessageBoard.GuestMessageBoardCommands;
using benandkatiegetmarried.DAL.GuestMessageBoard.GuestMessageBoardQueries;
using benandkatiegetmarried.Models;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace benandkatiegetmarried.Modules.GuestModules
{
    public class GuestMessageBoard: NancyModule
    { 
        private IGuestMessageBoardQueries _queries;
        private IGuestMessageBoardCommands _commands;
        private IValidator<Message> _messageValidator;

        public GuestMessageBoard(IGuestMessageBoardQueries queries
            , IGuestMessageBoardCommands commands
            , IValidator<Message> messageValidator) : base("api/guest/{eventId}/messageboard")
        {
            this.RequiresAuthentication();
            this.RequiresClaims("Guest");

            _queries = queries;
            _commands = commands;
            _messageValidator = messageValidator;

            Get["/"] = p => GetMessageBoards(p.eventId);
            Get["/{messageBoardId}/messages"] = p => GetMessages(p.messageBoardId);
            Post["/{messageBoardId}"] = _ => PostMessage();
            Delete["/{messageBoardId}/messages/{messageId}"] = p => DeleteMessage(p.messageBoardId, p.messageId);
            Put["/{messageBoardId}/messages/{messageId}"] = _ => UpdateMessage();
        }

        private dynamic GetMessages(dynamic id)
        {
            var inviteQuery = this.Request.Query["for-invite"];
            if (IsGuid(id) && (inviteQuery == null || IsGuid(inviteQuery)))
            {
                var messages = inviteQuery
                ? _queries.GetMessagesFromInvite((Guid)id, (Guid)inviteQuery)
                : _queries.GetMessages((Guid)id);

                var messageIds = messages.Select(x => x.Id);
                var likes = _queries.GetLikes(messageIds);
                var attributions = _queries.GetAttributions(messageIds);

                map(messages, likes, (m,g) => m.Likes.Add(g));
                map(messages, attributions, (m, g) => m.Attributions.Add(g));

                return messages;
            }
            return ErrorResponse.FromError(
                new Error() { ErrorMessage = "Message Board Id and Invite Query must be Guids" })
                .WithStatusCode(HttpStatusCode.BadRequest);
        }

        private void map(IEnumerable<Message> messages, IEnumerable<MessageGuest> likes, Action<Message, MessageGuest> action)
        {
            foreach (var message in messages)
            {
                foreach (var like in likes)
                {
                    if (message.Id == like.MessageId)
                    {
                        action.Invoke(message, like);
                    }
                }
            }
        }

        private bool IsGuid(dynamic possibleGuid)
        {
            Guid result;
            if (Guid.TryParse(possibleGuid, out result))
            {
                return true;
            }
            return false;
        }

        private dynamic GetMessageBoards(string eventId)
        {
            Guid eventIdGuid;
            Guid.TryParse(eventId, out eventIdGuid);
            if(eventIdGuid != null)
            {
                var response = _queries.GetMessageBoards(eventIdGuid);
                return response;
            }
            return new TextResponse("EventId is not a valid guid")
                .WithStatusCode(HttpStatusCode.BadRequest);
        }

        private dynamic UpdateMessage()
        {
            var request = this.Bind<Message>();
            var validationResult = _messageValidator.Validate(request);
            if (validationResult.IsValid)
            {
                _commands.Update(request);
                return HttpStatusCode.NoContent;
            }
            return HttpStatusCode.BadRequest;
        }

        private object DeleteMessage(dynamic messageBoardId, dynamic messageId)
        {
            if(IsGuid(messageBoardId) && IsGuid(messageId))
            {
                _commands.Delete((Guid)messageBoardId, (Guid)messageId);
                return HttpStatusCode.NoContent;
            }
            return HttpStatusCode.BadRequest;
        }

        private dynamic PostMessage()
        {
            var request = this.Bind<Message>();
            request.MessageBoardId = this.Context.Parameters["messageBoardId"];

            var validationResult = _messageValidator.Validate(request);
            if (validationResult.IsValid)
            {
                _commands.Create(request);
                return HttpStatusCode.NoContent;
            }
            return HttpStatusCode.BadRequest;
        }

    }
}

