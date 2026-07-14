CREATE TABLE [dbo].[tCollects] (
    [CollectID]   INT      IDENTITY (10000001, 1) NOT NULL,
    [MemberID]    INT      NOT NULL,
    [TypeID]      INT      NOT NULL,
    [ObjectID]    INT      NOT NULL,
    [CollectTime] DATETIME NOT NULL,
    [DemoSessionID] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Collects] PRIMARY KEY CLUSTERED ([CollectID] ASC)
);

GO

CREATE INDEX [IX_tCollects_MemberID_DemoSessionID_TypeID_ObjectID]
    ON [dbo].[tCollects] ([MemberID], [DemoSessionID], [TypeID], [ObjectID]);
