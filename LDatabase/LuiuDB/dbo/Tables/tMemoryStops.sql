CREATE TABLE [dbo].[tMemoryStops] (
    [StopID]        INT            IDENTITY (10000001, 1) NOT NULL,
    [DayID]         INT            NOT NULL,
    [PlaceName]     NVARCHAR (100) NOT NULL,
    [Latitude]      DECIMAL (9, 6) NOT NULL,
    [Longitude]     DECIMAL (9, 6) NOT NULL,
    [ArrivalTime]   DATETIME2 (7)  NULL,
    [MemoryText]    NVARCHAR (MAX) NULL,
    [VideoEmbedURL] VARCHAR (255)  NULL,
    [Duration]      NVARCHAR (50)  NULL,
    [Expense]       DECIMAL (18, 2) NULL,
    [Rating]        INT            NULL,
    [ImageUrls] NVARCHAR(MAX) NULL, 
    PRIMARY KEY CLUSTERED ([StopID] ASC)
);