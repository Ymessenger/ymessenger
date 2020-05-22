DROP TYPE IF EXISTS ConversationPreview CASCADE;
CREATE TYPE ConversationPreview AS
(
  "ConversationType" integer,
  "ConversationId" bigint,
  "Title" text,
  "Photo" text,
  "PreviewText" text,
  "UnreadedCount" bigint,
  "LastMessageSenderId" bigint,
  "LastMessageSenderName" text,
  "LastMessageTime" bigint,
  "SecondUserId" bigint,
  "Read" boolean,
  "AttachmentTypes" smallint[],
  "LastMessageId" UUID,
  "IsMuted" boolean
);
CREATE OR REPLACE FUNCTION get_user_conversations(userId bigint) RETURNS setof ConversationPreview AS $$
  DECLARE 
    Conversation RECORD;    
  BEGIN
    FOR Conversation IN
      SELECT
        1,
        dialog."Id",
        "dialogUser"."NameFirst" || ' ' || "dialogUser"."NameSecond" :: text as dialogName,
        "dialogUser"."Photo" :: text,
        "dialogMessage"."Text" :: text,
        COUNT("unreadedMessage"),
        "dialogMessage"."SenderId",
        NULL,
        "dialogMessage"."SendingTime",
        "dialogUser"."Id",
        "dialogMessage"."Read",
        ARRAY_REMOVE( ARRAY_AGG("dialogAttachment"."Type"), NULL),
        dialog."LastMessageGlobalId",
        dialog."IsMuted"
      FROM 
        "Dialogs" as dialog
          LEFT OUTER JOIN "Messages" AS "dialogMessage" ON (dialog."LastMessageGlobalId" = "dialogMessage"."GlobalId") AND (dialog."Id" = "dialogMessage"."DialogId")       
          LEFT JOIN "Attachments" AS "dialogAttachment" ON "dialogMessage"."Id" = "dialogAttachment"."MessageId"
          LEFT JOIN "Users" AS "dialogUser" ON dialog."SecondUID" = "dialogUser"."Id"
          LEFT OUTER JOIN "Messages" AS "unreadedMessage" ON dialog."Id" = "unreadedMessage"."DialogId" AND ("unreadedMessage"."SenderId"<> userId AND "unreadedMessage"."Read" = FALSE)
      WHERE 
        dialog."FirstUID" = userId         
        AND ("dialogMessage"."Deleted" = FALSE OR "dialogMessage" IS NULL)                  
      GROUP BY 
          dialog."Id", 
          "dialogMessage"."SenderId", 
          dialogName, 
          "dialogMessage"."SendingTime", 
          "dialogMessage"."Read",
          dialog."IsMuted", 
          "dialogUser"."Photo", 
          "dialogMessage"."Text",
          "dialogUser"."Id"
      UNION
      SELECT 
        2,
        chat."Id",
        chat."Name" :: text,
        chat."Photo" :: text,
        "chatMessage"."Text" :: text,
        COUNT("unreadedMessage"),
        "chatMessage"."SenderId",
        "chatMessageSender"."NameFirst" || ' ' || "chatMessageSender"."NameSecond" :: text as senderName,
        "chatMessage"."SendingTime",
        NULL,
        "chatMessage"."Read",
        ARRAY_REMOVE(  ARRAY_AGG("chatAttachment"."Type"), NULL),
        chat."LastMessageGlobalId",
        "chatUser"."IsMuted"
      FROM 
        "ChatUsers" AS "chatUser"
          LEFT JOIN "Chats" AS chat ON "chatUser"."ChatId" = chat."Id"
          LEFT JOIN "Messages" AS "lastReadedChatMessage" ON ("chatUser"."LastReadedGlobalMessageId" = "lastReadedChatMessage"."GlobalId") AND ("chatUser"."ChatId" = "lastReadedChatMessage"."ChatId")
          LEFT OUTER JOIN "Messages" AS "chatMessage" ON (chat."LastMessageGlobalId" = "chatMessage"."GlobalId") AND (chat."Id" = "chatMessage"."ChatId")
          LEFT JOIN "Attachments" AS "chatAttachment" ON "chatMessage"."Id" = "chatAttachment"."MessageId"
          LEFT JOIN "Users" AS "chatMessageSender" ON "chatMessage"."SenderId" = "chatMessageSender"."Id"
          LEFT OUTER JOIN "Messages" AS "unreadedMessage" ON chat."Id" = "unreadedMessage"."ChatId" 
              AND ("lastReadedChatMessage"."SendingTime" < "unreadedMessage"."SendingTime" AND "unreadedMessage"."SenderId" <> userId)
      WHERE 
        "chatUser"."Deleted" = FALSE 
        AND "chatUser"."Banned" = FALSE 
        AND "chatUser"."UserId" = userId 
        AND chat."Deleted" = FALSE
        AND ("chatMessage" IS NULL OR "chatMessage"."Deleted" = FALSE)
        AND ("unreadedMessage" IS NULL OR "unreadedMessage"."Deleted" = FALSE)
      GROUP BY 
          chat."Id", 
          "chatMessage"."Text", 
          "chatMessage"."SenderId", 
          senderName, 
          "chatMessage"."SendingTime", 
          "chatMessage"."Read", 
          "chatUser"."IsMuted"
      UNION
      SELECT 
        3,
        channel."ChannelId",
        channel."ChannelName" :: text,
        channel."Photo" :: text,
        "channelMessage"."Text" :: text,
        COUNT("unreadedMessage"),
        "channelMessage"."SenderId",
        channel."ChannelName" :: text,
        "channelMessage"."SendingTime",
        NULL,
        "channelMessage"."Read",
        ARRAY_REMOVE(  ARRAY_AGG("channelAttachment"."Type"), NULL ),
        channel."LastMessageGlobalId",
        "channelUser"."IsMuted"
      FROM
        "ChannelUsers" AS "channelUser"
          LEFT JOIN "Channels" AS channel ON "channelUser"."ChannelId" = channel."ChannelId"
          LEFT JOIN "Messages" AS "lastReadedChannelMessage" ON ("channelUser"."LastReadedGlobalMessageId" = "lastReadedChannelMessage"."GlobalId") AND ("channelUser"."ChannelId" = "lastReadedChannelMessage"."ChannelId")
          LEFT OUTER JOIN "Messages" AS "channelMessage" ON (channel."LastMessageGlobalId" = "channelMessage"."GlobalId") AND (channel."ChannelId" = "channelMessage"."ChannelId")
          LEFT JOIN "Attachments" AS "channelAttachment" ON "channelMessage"."Id" = "channelAttachment"."MessageId" 
          LEFT OUTER JOIN "Messages" AS "unreadedMessage" ON channel."ChannelId" = "unreadedMessage"."ChannelId"
                AND ("lastReadedChannelMessage"."SendingTime" < "unreadedMessage"."SendingTime" AND "unreadedMessage"."SenderId" <> userId)
      WHERE 
        "channelUser"."UserId" = userId
        AND "channelUser"."Deleted" = FALSE
        AND "channelUser"."Banned" = FALSE
        AND channel."Deleted" = FALSE
        AND ("channelMessage" IS NULL OR "channelMessage"."Deleted" = FALSE)
        AND ("unreadedMessage" IS NULL OR "unreadedMessage"."Deleted" = FALSE)
      GROUP BY
           channel."ChannelId", 
          "channelMessage"."SenderId", 
           channel."ChannelName", 
          "channelMessage"."SendingTime", 
          "channelMessage"."Read",
          "channelUser"."IsMuted",           
          "channelMessage"."Text"
  LOOP 
    RETURN NEXT Conversation;
  END LOOP;
END $$ LANGUAGE plpgsql;