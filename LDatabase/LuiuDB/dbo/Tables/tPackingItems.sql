CREATE TABLE [dbo].[tPackingItems] (
    [ItemID]     INT           IDENTITY (10000001, 1) NOT NULL,
    [ItemName]   NVARCHAR (20) NOT NULL,
    [IsCheck]    BIT           DEFAULT ((0)) NULL,
    [CategoryID] INT           NOT NULL,
    [IsDeleted]  BIT           DEFAULT ((0)) NOT NULL,
    [CreateAt]   DATETIME      DEFAULT (getdate()) NOT NULL,
    [UpdateAt]   DATETIME      DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([ItemID] ASC)
);

