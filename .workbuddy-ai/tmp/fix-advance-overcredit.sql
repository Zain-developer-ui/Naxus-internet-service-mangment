SET NOCOUNT ON;

/*
   INV-2026-00001 was raised before the advance was capped at the period's
   own charges. Rental 315 was credited with the full 500 security deposit,
   which dragged the line total to -110 and the tax to -13.46 - a bill that
   reads as a refund the customer never asked for.

   The advance on this connection is worth 500. Against this bill it can only
   absorb 315 of rental, so 315 is what belongs here and the remaining 185
   stays on the order for the next period.
*/

UPDATE BillLines SET LineAmount = -315.00, UnitAmount = -315.00
WHERE Id = 8 AND BillId = 6;

UPDATE ConnectionOrders SET AdvanceApplied = 315.00, AdvanceAdjustedOn = NULL
WHERE Id = 21;

DECLARE @subtotal decimal(18,2) = (SELECT SUM(LineAmount) FROM BillLines WHERE BillId = 6);
DECLARE @taxable  decimal(18,2) = @subtotal;
DECLARE @tax      decimal(18,2) = CAST(ROUND(CASE WHEN @taxable > 0 THEN @taxable * 0.1224 ELSE 0 END, 2) AS decimal(18,2));

UPDATE Bills SET
    SubTotal         = @subtotal,
    DiscountAmount   = 0,
    TaxableAmount    = @taxable,
    ServiceTaxAmount = @tax,
    TotalAmount      = CASE WHEN @taxable + @tax > 0 THEN @taxable + @tax ELSE 0 END
WHERE Id = 6;
