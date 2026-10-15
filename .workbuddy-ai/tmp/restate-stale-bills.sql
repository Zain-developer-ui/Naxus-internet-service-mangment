SET NOCOUNT ON;

/*
   Bills raised before the one-credit-per-connection rule carried the advance
   twice. c27/c28 already had 350 of their 500 drawn down, so their October
   bill (7-day rental, 175/month prorated) should have been settled by the
   advance and the November bill (10, 11) should have carried no credit at all.
   Quote those stored totals first: if a bill has been paid on the old, wrong
   total it is left alone rather than quietly restated.
*/

DECLARE @bills TABLE (BillId int, FallbackTotal decimal(18,2));
INSERT INTO @bills (BillId, FallbackTotal) VALUES (7, 0.00), (8, 0.00), (9, 0.00), (11, 0.00);

DECLARE @billId int, @fallback decimal(18,2);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT BillId, FallbackTotal FROM @bills;
OPEN cur;
FETCH NEXT FROM cur INTO @billId, @fallback;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF (SELECT AmountPaid FROM Bills WHERE Id = @billId) > 0
    BEGIN
        PRINT 'Bill ' + CAST(@billId AS varchar(10)) + ' has payments - left as is.';
    END
    ELSE
    BEGIN
        DECLARE @subtotal decimal(18,2) =
            ISNULL((SELECT SUM(LineAmount) FROM BillLines WHERE BillId = @billId), 0);
        DECLARE @taxable decimal(18,2) = @subtotal;
        DECLARE @tax decimal(18,2) =
            CAST(ROUND(CASE WHEN @taxable > 0 THEN @taxable * 0.1224 ELSE 0 END, 2) AS decimal(18,2));
        DECLARE @total decimal(18,2) =
            CASE WHEN @taxable + @tax > 0 THEN @taxable + @tax ELSE @fallback END;

        UPDATE Bills SET
            SubTotal         = @subtotal,
            DiscountAmount   = 0,
            TaxableAmount    = @taxable,
            ServiceTaxAmount = @tax,
            TotalAmount      = @total
        WHERE Id = @billId;

        PRINT 'Bill ' + CAST(@billId AS varchar(10)) + ' restated to ' + CAST(@total AS varchar(20));
    END

    FETCH NEXT FROM cur INTO @billId, @fallback;
END

CLOSE cur;
DEALLOCATE cur;
