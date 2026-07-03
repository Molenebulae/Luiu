CREATE TABLE [dbo].[tExpensesSplits] (
    [SplitID]      INT             IDENTITY (10000001, 1) NOT NULL,
    [ExpenseID]    INT             NOT NULL,
    [DebtorUserID] INT             NOT NULL,
    [OweAmount]    DECIMAL (10, 2) NOT NULL,
    PRIMARY KEY CLUSTERED ([SplitID] ASC)
);

