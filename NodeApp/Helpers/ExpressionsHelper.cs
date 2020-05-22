/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace NodeApp.Helpers
{
    public class ExpressionsHelper
    {
        public Expression<Func<User, bool>> GetUserExpression(SearchUserVm templateUser)
        {
            ExpressionStarter<User> userCondition = PredicateBuilder.New<User>();
            if (!string.IsNullOrWhiteSpace(templateUser.NameFirst))
            {
                userCondition = userCondition.And(user => user.NameFirst.ToLower() == templateUser.NameFirst.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(templateUser.NameSecond))
            {
                userCondition = userCondition.And(user => user.NameSecond.ToLower() == templateUser.NameSecond.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(templateUser.City))
            {
                userCondition = userCondition.And(user => user.City.ToLower() == templateUser.City.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(templateUser.Country))
            {
                userCondition = userCondition.And(user => user.Country.ToLower() == templateUser.Country.ToLower());
            }
            if (templateUser.Birthday != null)
            {
                userCondition = userCondition.And(user => user.Birthday == templateUser.Birthday);
            }
            if (templateUser.Tag != null)
            {
                userCondition = userCondition.And(user => user.Tag == templateUser.Tag);
            }
            return (Expression<Func<User, bool>>)userCondition.Expand();
        }

        public Expression<Func<Chat, bool>> GetChatExpression(SearchChatVm templateChat)
        {
            ExpressionStarter<Chat> chatCondition = PredicateBuilder.New<Chat>();
            if (!string.IsNullOrWhiteSpace(templateChat.Name))
            {
                chatCondition = chatCondition.And(chat => chat.Name.ToLower() == templateChat.Name.ToLower());
            }
            if (templateChat.Tag != null)
            {
                chatCondition = chatCondition.And(chat => chat.Tag == templateChat.Tag);
            }
            return (Expression<Func<Chat, bool>>)chatCondition.Expand();
        }

        public Expression<Func<User, bool>> GetUserExpression(string query)
        {
            ExpressionStarter<User> userCondition = PredicateBuilder.New<User>();
            string lowerQuery = query.ToLowerInvariant();
            userCondition = userCondition.Or(user =>
                    user.Phones.Any(opt => opt.PhoneNumber == query)
                || user.Emails.Any(opt => opt.EmailAddress.ToLower() == query.ToLowerInvariant())
                || user.SearchVector.Matches(EF.Functions.PhraseToTsQuery("simple", query)));
            return (Expression<Func<User, bool>>)userCondition.Expand();
        }

        public Expression<Func<Chat, bool>> GetChatExpression(string query)
        {
            ExpressionStarter<Chat> chatCondition = PredicateBuilder.New<Chat>();
            string lowerQuery = query.ToLowerInvariant();
            chatCondition = chatCondition.Or(chat => chat.SearchVector.Matches(EF.Functions.PhraseToTsQuery("simple", query)));
            return (Expression<Func<Chat, bool>>)chatCondition.Expand();
        }

        public Expression<Func<Channel, bool>> GetChannelExpression(string query)
        {
            ExpressionStarter<Channel> chatCondition = PredicateBuilder.New<Channel>();
            string lowerQuery = query.ToLowerInvariant();
            chatCondition = chatCondition.Or(chat => chat.SearchVector.Matches(EF.Functions.PhraseToTsQuery("simple", query)));
            return (Expression<Func<Channel, bool>>)chatCondition.Expand();
        }
        public Expression<Func<Message, bool>> GetMessageExpression(string query)
        {
            var messageCondition = PredicateBuilder.New<Message>();
            messageCondition = messageCondition.Or(message => message.SearchVector.Matches(EF.Functions.PhraseToTsQuery("russian", query))
                || message.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", query))
                || message.SearchVector.Matches(EF.Functions.PhraseToTsQuery("simple", query)));
            return (Expression<Func<Message, bool>>)messageCondition.Expand();
        }
    }
}
