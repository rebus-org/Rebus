USE [rebus_test]
GO

CREATE TABLE [dbo].[sagas](
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_sagas] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE [dbo].[saga_index](
	[saga_type] [nvarchar](40) NOT NULL,
	[key] [nvarchar](200) NOT NULL,
	[value] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_saga_index] PRIMARY KEY CLUSTERED 
(
	[key] ASC,
	[value] ASC,
	[saga_type] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO