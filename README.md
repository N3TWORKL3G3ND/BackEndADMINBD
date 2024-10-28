# BackEndADMINBD

ES IMPORTANTE APLICAR LOS SIGUIENTES COMANDOS EN SQL DEVELORES CONECTADOS A SYS:

CREATE OR REPLACE DIRECTORY DATA_PUMP_DIR AS 'C:\ADMINBD\RESPALDOS';
GRANT READ, WRITE ON DIRECTORY DATA_PUMP_DIR TO SYS;

Para poder trabajar con respaldos.


    //======================================
    //===============TUNNING================
    //======================================
	
Para poder trabajar con la API de crear planes de ejcucion tienen que enviar una consulta SQL
dicha consulta debe venir sin ";" al final o la consulta no funcionara.

Ejemplo de consulta:    SELECT * FROM PADRON.ALAJUELA WHERE CEDULA = '123456789'

