CREATE TABLE [dbo].[tExpenses] (
    [ExpenseID]   INT             IDENTITY (1000001, 1) NOT NULL,
    [MemoryID]    INT             NOT NULL,
    [PayerUserID] INT             NOT NULL,
    [Title]       NVARCHAR (100)  NOT NULL,
    [TotalAmount] DECIMAL (10, 2) NOT NULL,
    PRIMARY KEY CLUSTERED ([ExpenseID] ASC)
);

