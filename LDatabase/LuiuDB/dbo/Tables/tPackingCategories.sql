CREATE TABLE [dbo].[tPackingCategories] (
    [CategoryID]   INT           IDENTITY (10000001, 1) NOT NULL,
    [CategoryName] NVARCHAR (20) NOT NULL,
    [ListID]       INT           NOT NULL,
    [IsDeleted]    BIT           DEFAULT ((0)) NOT NULL,
    [CreateAt]     DATETIME      DEFAULT (getdate()) NOT NULL,
    [UpdateAt]     DATETIME      DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([CategoryID] ASC)
);

