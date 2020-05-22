CREATE OR REPLACE FUNCTION exchange_message_ids(m_first_id bigint, m_second_id bigint) RETURNS VOID as $$  
  BEGIN
	IF m_second_id IS NOT NULL THEN
		EXECUTE 'UPDATE "Messages" SET "SameMessageId"= ' || m_second_id || ' where "Id"= ' || m_first_id ;
		EXECUTE 'UPDATE "Messages" SET "SameMessageId"= ' || m_first_id || ' where "Id"= ' || m_second_id ;
	END IF;
  END $$
  LANGUAGE plpgsql;