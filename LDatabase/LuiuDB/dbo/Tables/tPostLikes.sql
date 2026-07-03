CREATE TABLE [dbo].[tPostLikes] (
    [PostLikeID] INT      IDENTITY (10000001, 1) NOT NULL,
    [PostID]     INT      NOT NULL,
    [MemberID]   INT      NOT NULL,
    [LikeTime]   DATETIME DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([PostLikeID] ASC)
);

