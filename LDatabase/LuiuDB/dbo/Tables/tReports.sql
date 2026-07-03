CREATE TABLE [dbo].[tReports] (
    [ReportID]     INT            IDENTITY (1000001, 1) NOT NULL,
    [ReportReason] NVARCHAR (255) NOT NULL,
    [Status]       TINYINT        CONSTRAINT [DF_Reports_Status] DEFAULT ((0)) NOT NULL,
    [CreateTime]   DATETIME       CONSTRAINT [DF_Reports_CreateTime] DEFAULT (getdate()) NOT NULL,
    [TargetType]   VARCHAR (20)   NOT NULL,
    [TargetID]     INT            NOT NULL,
    [ReporterID]   INT            NOT NULL,
    [RuleID]       INT            NOT NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportID] ASC)
);

