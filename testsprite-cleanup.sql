/* TestSprite run se pehle chalao.

   Tests fixed naam use karte hain ("E2E Probe Vendor", "E2E Probe City").
   Agar pichhle run ke records mojood hon to vendor aur city screens duplicate
   naam reject kar dete hain, aur test code ke bajaye data par fail hota hai.
   Is liye har run se pehle ye script chala kar slate saaf karo.

   Chalane ka tareeqa:
     sqlcmd -S DESKTOP-BD6OLPC\SQLEXPRESS -U sa -P aptech -d NexusDb -i testsprite-cleanup.sql
*/

SET NOCOUNT ON;

BEGIN TRANSACTION;

-- Edit-plan test jo bhi plan khole uske naam ke aage "Probe" laga deta hai.
UPDATE Plans SET Name = 'Dial-Up 10 Hours'
WHERE Id = 1 AND Name = 'ProbeDial-Up 10 Hours';

-- Outlet test is seeded employee ko apne outlet par assign kar deta hai.
UPDATE Employees SET ShopId = NULL
WHERE Id = 6 AND ShopId IN (SELECT Id FROM RetailShops WHERE Name LIKE '%E2E%');

-- Staff-login screen ke manual probes se bane logins.
DELETE FROM Users WHERE Email LIKE '%.probe@nexus.example';

-- Test ke banaye records, pehle bachay hue, phir walid.
DELETE FROM PlanPrices WHERE PlanId IN (SELECT Id FROM Plans WHERE Name LIKE '%E2E%');
DELETE FROM Plans WHERE Name LIKE '%E2E%';

DELETE FROM RetailShops WHERE Name LIKE '%E2E%';

UPDATE EquipmentProducts SET PreferredVendorId = NULL
WHERE PreferredVendorId IN (SELECT Id FROM Vendors WHERE Name LIKE '%E2E%' OR Name LIKE '%Probe%');
DELETE FROM EquipmentProducts WHERE Name LIKE '%E2E%';

DELETE FROM Vendors WHERE Name LIKE '%E2E%' OR Name LIKE '%Probe%';

DELETE FROM Cities WHERE Name LIKE '%E2E%';

COMMIT;

SELECT 'Plans' AS T, COUNT(*) AS N FROM Plans
UNION ALL SELECT 'Vendors', COUNT(*) FROM Vendors
UNION ALL SELECT 'EquipmentProducts', COUNT(*) FROM EquipmentProducts
UNION ALL SELECT 'Cities', COUNT(*) FROM Cities
UNION ALL SELECT 'RetailShops', COUNT(*) FROM RetailShops
UNION ALL SELECT 'Employees', COUNT(*) FROM Employees;
