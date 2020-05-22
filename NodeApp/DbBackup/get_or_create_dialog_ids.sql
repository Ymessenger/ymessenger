CREATE OR REPLACE FUNCTION get_or_create_dialog_ids(senderid bigint, receiverid bigint) RETURNS setof bigint AS $$
  DECLARE
    strquery VARCHAR(300);
    reservedquery VARCHAR(300);  
    
  BEGIN
    strquery := FORMAT('SELECT "Id" FROM "Dialogs" WHERE ("FirstUID" = %1$s AND "SecondUID"=%2$s) OR ("FirstUID" = %2$s AND "SecondUID"=%1$s)',
                      senderid, receiverid);    
    RETURN QUERY EXECUTE strquery;
    IF NOT FOUND THEN
      IF senderid = receiverid THEN
       reservedquery := FORMAT('INSERT INTO "Dialogs"("FirstUID", "SecondUID") VALUES(%1$s, %2$s) RETURNING "Id"', senderid, receiverid);
      ELSE
       reservedquery := FORMAT('INSERT INTO "Dialogs"("FirstUID", "SecondUID") VALUES(%1$s, %2$s),(%2$s, %1$s) RETURNING "Id"', senderid, receiverid); 
      END IF;   
      RETURN QUERY EXECUTE reservedquery;          
    END IF; 
    RETURN; 
  END  $$
LANGUAGE plpgsql;