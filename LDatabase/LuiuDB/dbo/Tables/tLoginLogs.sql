CREATE TABLE [dbo].[tLoginLogs] (
    [LoginID]       INT            IDENTITY (100000001, 1) NOT NULL,
    [LoginTime]     DATETIME       CONSTRAINT [DF_LoginLogs_LoginTime] DEFAULT (getdate()) NOT NULL,
    [IPAddress]     VARCHAR (50)   NOT NULL,
    [Status]        VARCHAR (20)   NOT NULL,
    [Location]      NVARCHAR (100) NULL,
    [DeviceInfo]    NVARCHAR (MAX) NOT NULL,
    [FailureReason] NVARCHAR (50)  NOT NULL,
    [MemberID]      INT            NOT NULL,
    CONSTRAINT [PK_LoginLogs] PRIMARY KEY CLUSTERED ([LoginID] ASC)
);

