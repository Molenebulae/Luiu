CREATE TABLE [dbo].[tCommentLikes] (
    [CommentLikeID] INT IDENTITY (10000001, 1) NOT NULL,
    [CommentID]     INT NOT NULL,
    [MemberID]      INT NOT NULL,
    PRIMARY KEY CLUSTERED ([CommentLikeID] ASC)
);

