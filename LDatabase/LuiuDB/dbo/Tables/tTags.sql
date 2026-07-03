CREATE TABLE [dbo].[tTags] (
    [TagID]   INT           IDENTITY (10000001, 1) NOT NULL,
    [TagName] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([TagID] ASC)
);

