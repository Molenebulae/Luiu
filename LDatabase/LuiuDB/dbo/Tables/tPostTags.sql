CREATE TABLE [dbo].[tPostTags] (
    [PostTagID] INT IDENTITY (10000001, 1) NOT NULL,
    [PostID]    INT NOT NULL,
    [TagID]     INT NOT NULL,
    PRIMARY KEY CLUSTERED ([PostTagID] ASC)
);

