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
using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NodeApp.Migrations.MessengerDb
{
    public partial class AddMessageNodesIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long[]>(
                name: "NodesIds",
                table: "Messages",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""Messages"" as updating SET 
                                   ""NodesIds"" = ARRAY_REMOVE(ARRAY(SELECT DISTINCT UNNEST(subquery.senderNode || (subquery.receiverNode || subquery.chatnodes || subquery.channelnodes)) ORDER BY 1), NULL)
                                  FROM
                                  (
                                    SELECT
                                        mess.""Id"",
                                        sender.""NodeId"" as senderNode,
                                        receiver.""NodeId"" as receiverNode,
                                        chat.""NodesId"" as chatNodes,
                                        channel.""NodesId"" as channelNodes
                                    FROM ""Messages"" AS mess
                                        LEFT JOIN ""Chats"" as chat on mess.""ChatId"" = chat.""Id""
                                        LEFT JOIN ""Channels"" as channel on mess.""ChannelId"" = channel.""ChannelId""
                                        LEFT JOIN ""Users"" as sender on mess.""SenderId"" = sender.""Id""
                                        LEFT JOIN ""Users"" as receiver on mess.""ReceiverId"" = receiver.""Id""
                                  ) as subquery
                                WHERE updating.""Id"" = subquery.""Id""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NodesIds",
                table: "Messages");
        }
    }
}
