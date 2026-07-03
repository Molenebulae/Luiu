CREATE TABLE [dbo].[tViolationRules] (
    [RuleID]        INT            IDENTITY (101, 1) NOT NULL,
    [Title]         NVARCHAR (50)  NOT NULL,
    [Description]   NVARCHAR (200) NOT NULL,
    [RuleType]      NVARCHAR (10)  NOT NULL,
    [IsActivate]    BIT            CONSTRAINT [DF__Violation__IsAct__793DFFAF] DEFAULT ((1)) NOT NULL,
    [Severity]      TINYINT        CONSTRAINT [DF__Violation__Sever__7A3223E8] DEFAULT ((3)) NOT NULL,
    [DefaultAction] NVARCHAR (50)  NULL,
    [UpdatedAt]     DATETIME       CONSTRAINT [DF__Violation__Updat__7B264821] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ViolationRules] PRIMARY KEY CLUSTERED ([RuleID] ASC)
);

