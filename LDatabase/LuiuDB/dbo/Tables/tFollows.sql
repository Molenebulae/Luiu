CREATE TABLE [dbo].[tFollows] (
    [FollowerID]  INT      NOT NULL,
    [FollowingID] INT      NOT NULL,
    [CreateTime]  DATETIME CONSTRAINT [DF_Follows_CreateTime] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Follows] PRIMARY KEY CLUSTERED ([FollowerID] ASC, [FollowingID] ASC)
);

