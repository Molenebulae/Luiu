CREATE TABLE [dbo].[tEvents] (
    [EventID]       INT            IDENTITY (100001, 1) NOT NULL,
    [EventName]     NVARCHAR (50)  NOT NULL,
    [Description]   NVARCHAR (MAX) NULL,
    [StartTime]     DATETIME       NULL,
    [EndTime]       DATETIME       NULL,
    [EventType]     NVARCHAR (50)  NULL,
    [ImageBG]       NVARCHAR (MAX) NULL,
    [OfficialURL]   NVARCHAR (MAX) NULL,
    [CreateTime]    DATETIME       CONSTRAINT [DF_Event_CreateTime] DEFAULT (getdate()) NOT NULL,
    [ViewCount]     INT            CONSTRAINT [DF_Event_ViewCount] DEFAULT ((0)) NOT NULL,
    [FavoriteCount] INT            CONSTRAINT [DF_Event_FavoriteCount] DEFAULT ((0)) NOT NULL,
    [PlanCount]     INT            CONSTRAINT [DF_Event_PlanCount] DEFAULT ((0)) NOT NULL,
    [RecordCount]   INT            CONSTRAINT [DF_Event_RecordCount] DEFAULT ((0)) NOT NULL,
    [RefCount]      INT            CONSTRAINT [DF_Event_RefCount] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([EventID] ASC)
);

