CREATE TABLE [dbo].[tTrips] (
    [TripID]        INT              IDENTITY (1000001, 1) NOT NULL,
    [TripName]      NVARCHAR (20)    NOT NULL,
    [StartDate]     DATE             NOT NULL,
    [EndDate]       DATE             NOT NULL,
    [PrivacyStatus] TINYINT          DEFAULT ((0)) NULL,
    [IsSuggest]     BIT              DEFAULT ((0)) NULL,
    [TripGuid]      UNIQUEIDENTIFIER DEFAULT (newid()) NULL,
    [ShortCode]     NVARCHAR (8)     NULL,
    [OwnerID]       INT              NOT NULL,
    [ListID]        INT              NULL,
    [IsDeleted]     BIT              DEFAULT ((0)) NOT NULL,
    [CreateAt]      DATETIME         DEFAULT (getdate()) NOT NULL,
    [UpdateAt]      DATETIME         DEFAULT (getdate()) NULL,
    [TripDesc]      NVARCHAR (50)    NULL,
    [OfficeOper]    SMALLINT         DEFAULT ((0)) NULL,
    [PhotoURL]      NVARCHAR (255)   NULL,
    [TripTag]       NVARCHAR (10)    NULL,
    PRIMARY KEY CLUSTERED ([TripID] ASC)
);

