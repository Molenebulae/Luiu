CREATE TABLE [dbo].[tSystemConfigs] (
    [ConfigKey]   NVARCHAR (50)  NOT NULL,
    [ConfigValue] NVARCHAR (MAX) NULL,
    [LastUpdated] DATETIME       CONSTRAINT [DF_SystemConfig_LastUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_SystemConfig] PRIMARY KEY CLUSTERED ([ConfigKey] ASC)
);

