CREATE TABLE [dbo].[tComments] (
    [CommentID]      INT            IDENTITY (10000001, 1) NOT NULL,
    [CommentContent] NVARCHAR (100) NOT NULL,
    [CommentTime]    DATETIME       DEFAULT (getdate()) NOT NULL,
    [ParentID]       INT            NULL,
    [PostID]         INT            NOT NULL,
    [MemberID]       INT            NOT NULL,
    [IsDelete]       BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([CommentID] ASC)
);

