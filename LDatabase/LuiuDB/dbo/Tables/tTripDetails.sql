CREATE TABLE [dbo].[tTripDetails] (
    [DetailID]      INT           IDENTITY (10000001, 1) NOT NULL,
    [SpotAlias]     NVARCHAR (20) NULL,
    [Notes]         NVARCHAR (50) NULL,
    [DayNumber]     TINYINT       NOT NULL,
    [SortOrder]     TINYINT       NOT NULL,
    [ArrivalTime]   TIME (7)      NULL,
    [StayDuration]  SMALLINT      DEFAULT ((60)) NULL,
    [TransportMode] TINYINT       NULL,
    [TransportTime] SMALLINT      NULL,
    [Budget]        DECIMAL (18, 2) NULL,
    [TripID]        INT           NOT NULL,
    [SpotID]        INT           NOT NULL,
    [VersionID]     TINYINT       DEFAULT ((0)) NULL,
    [IsMaster]      BIT           DEFAULT ((0)) NULL,
    [SuggestBy]     INT           NULL,
    [IsDeleted]     BIT           DEFAULT ((0)) NOT NULL,
    [CreateAt]      DATETIME      DEFAULT (getdate()) NOT NULL,
    [UpdateAt]      DATETIME      DEFAULT (getdate()) NULL,
    [PolylineEncoded] NVARCHAR(MAX) NULL, 
    PRIMARY KEY CLUSTERED ([DetailID] ASC)
);

