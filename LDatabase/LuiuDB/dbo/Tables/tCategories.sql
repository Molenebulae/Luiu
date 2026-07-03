CREATE TABLE [dbo].[tCategories] (
    [CategoryID]   INT           IDENTITY (1001, 1) NOT NULL,
    [CategoryName] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([CategoryID] ASC)
);

