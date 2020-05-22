CREATE OR REPLACE FUNCTION insert_dialog_messages(
  sender_id bigint,
  receiver_id bigint,   
  datesend bigint, 
  mtext varchar(10000),
  global_id uuid,
  replyto uuid default null,
  ltime bigint default null) 
RETURNS setof "Messages" as $$
  DECLARE   
    str_query varchar(10500);
    message RECORD;
    ids bigint[2];  	
    dialog_ids bigint[2];
    is_blocked BOOLEAN DEFAULT FALSE;    
  BEGIN 
  SELECT TRUE FROM "BadUsers" as B 
                       RIGHT JOIN "Users" as U on (B."UID" = U."Id")
                       WHERE ((U."Id"=sender_id AND B."BadUID"=receiver_id) 
                         OR (U."Id"=receiver_id AND B."BadUID"=sender_id)) 
                         OR (U."Confirmed"=false AND (U."Id"=sender_id OR U."Id"=receiver_id)) 
  LIMIT 1 INTO is_blocked;    
  IF is_blocked = 't' THEN
    RAISE EXCEPTION USING MESSAGE = 'The user is blacklisted by another user.', ERRCODE = 'BLOCK'; 
  END IF;
  dialog_ids := ARRAY(SELECT * FROM get_or_create_dialog_ids(sender_id, receiver_id))::bigint[];    
  IF sender_id = receiver_id THEN
    str_query := FORMAT('INSERT INTO "Messages"("SenderId", "SendingTime", "Replyto", "Text", "DialogId", "Read", "GlobalId", "ReceiverId", "ExpiredAt")' 
                        || 'VALUES(%1$s, %2$L, %3$L, %4$L, %5$s, false, %6$L, %7$L, %8$L) RETURNING *', 
                sender_id, datesend, replyto, mtext, dialog_ids[1], global_id, receiver_id, ltime);
  ELSE
  	str_query := FORMAT('INSERT INTO "Messages"("SenderId", "SendingTime", "Replyto", "Text", "DialogId", "Read", "GlobalId", "ReceiverId", "ExpiredAt")' 
                        || 'VALUES(%1$s, %2$L, %3$L, %4$L, %5$s, false, %7$L, %8$L, %9$L), (%1$s, %2$L, %3$L, %4$L, %6$s, false, %7$L, %8$L, %9$L) RETURNING *', 
                sender_id, datesend, replyto, mtext, dialog_ids[1], dialog_ids[2], global_id, receiver_id, ltime);
  END IF;
  FOR message IN EXECUTE str_query
  LOOP
    ids := ARRAY_APPEND(ids, message."Id");
    RETURN NEXT message;
  END LOOP;  
  EXECUTE exchange_message_ids(ids[1], ids[2]);  
  END $$ 
  LANGUAGE plpgsql;