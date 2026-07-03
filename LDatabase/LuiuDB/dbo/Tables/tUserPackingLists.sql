CREATE TABLE [dbo].[tUserPackingLists] (
    [ListID]    INT           IDENTITY (1000001, 1) NOT NULL,
    [ListName]  NVARCHAR (20) NOT NULL,
    [UserID]    INT           NOT NULL,
    [IsDeleted] BIT           DEFAULT ((0)) NOT NULL,
    [CreateAt]  DATETIME      DEFAULT (getdate()) NOT NULL,
    [UpdateAt]  DATETIME      DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([ListID] ASC)
);

