SET NOCOUNT ON;

/*
   The retail counter payment on INV-2026-00001 was a live end-to-end check of
   the new advance logic, not a real collection. It is rolled back so the bill
   stays Issued with a balance for tomorrow's demonstration.

   CON-B-000001 is PermanentlyInactive, so this bill stands only as a record of
   the period it covered - the connection was never live to be charged.
*/

DELETE FROM Payments WHERE Id = 7;

UPDATE Bills SET
    Status = 'Issued',
    AmountPaid = 0
WHERE Id = 6;
