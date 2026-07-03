CREATE TABLE [dbo].[tTripCollaborators] (
    [CollaboratorID] INT     IDENTITY (10000001, 1) NOT NULL,
    [Role]           TINYINT DEFAULT ((0)) NULL,
    [UserID]         INT     NOT NULL,
    [TripID]         INT     NOT NULL,
    PRIMARY KEY CLUSTERED ([CollaboratorID] ASC)
);

