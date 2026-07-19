CREATE TABLE [dbo].[tDemoSessions] (
    [DemoSessionID]         UNIQUEIDENTIFIER NOT NULL,
    [MemberID]              INT              NOT NULL,
    [ClientIpHash]          VARCHAR (128)    NULL,
    [UserAgentHash]         VARCHAR (128)    NULL,
    [StartedAt]             DATETIME         CONSTRAINT [DF_tDemoSessions_StartedAt] DEFAULT (getdate()) NOT NULL,
    [ExpiresAt]             DATETIME         NOT NULL,
    [EndedAt]               DATETIME         NULL,
    [EndReason]             NVARCHAR (30)    NULL,
    [PlaceSearchCount]      INT              CONSTRAINT [DF_tDemoSessions_PlaceSearchCount] DEFAULT ((0)) NOT NULL,
    [RouteComputeCount]     INT              CONSTRAINT [DF_tDemoSessions_RouteComputeCount] DEFAULT ((0)) NOT NULL,
    [RouteExternalLegCount] INT              CONSTRAINT [DF_tDemoSessions_RouteExternalLegCount] DEFAULT ((0)) NOT NULL,
    [CreatedTripCount]      INT              CONSTRAINT [DF_tDemoSessions_CreatedTripCount] DEFAULT ((0)) NOT NULL,
    [CreatedCollectCount]   INT              CONSTRAINT [DF_tDemoSessions_CreatedCollectCount] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_tDemoSessions] PRIMARY KEY CLUSTERED ([DemoSessionID] ASC)
);

GO

CREATE INDEX [IX_tDemoSessions_ExpiresAt_EndedAt]
    ON [dbo].[tDemoSessions] ([ExpiresAt], [EndedAt]);

GO

CREATE INDEX [IX_tDemoSessions_MemberID_ExpiresAt]
    ON [dbo].[tDemoSessions] ([MemberID], [ExpiresAt]);

GO

CREATE INDEX [IX_tDemoSessions_ClientIpHash_EndedAt_ExpiresAt]
    ON [dbo].[tDemoSessions] ([ClientIpHash], [EndedAt], [ExpiresAt]);
