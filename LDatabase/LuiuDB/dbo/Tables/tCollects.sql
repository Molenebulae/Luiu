CREATE TABLE [dbo].[tCollects] (
    [CollectID]   INT      IDENTITY (10000001, 1) NOT NULL,
    [MemberID]    INT      NOT NULL,
    [TypeID]      INT      NOT NULL,
    [ObjectID]    INT      NOT NULL,
    [CollectTime] DATETIME NOT NULL,
    CONSTRAINT [PK_Collects] PRIMARY KEY CLUSTERED ([CollectID] ASC)
);

