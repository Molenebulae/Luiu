CREATE TABLE [dbo].[tAdminLogs] (
    [ActionID]     INT            IDENTITY (1000001, 1) NOT NULL,
    [ActionType]   VARCHAR (50)   NOT NULL,
    [ActionReason] NVARCHAR (255) NOT NULL,
    [IpAddress]    VARCHAR (50)   NOT NULL,
    [ActionTime]   DATETIME       CONSTRAINT [DF_AdminLogs_ActionTime] DEFAULT (getdate()) NOT NULL,
    [PenaltyTime]  DATETIME       NOT NULL,
    [TargetType]   VARCHAR (20)   NOT NULL,
    [TargetID]     INT            NOT NULL,
    [AdminID]      INT            NOT NULL,
    [ReportID]     INT            NULL,
    CONSTRAINT [PK_AdminLogs] PRIMARY KEY CLUSTERED ([ActionID] ASC)
);

