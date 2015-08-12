USE [rebus_test]
GO

CREATE TABLE [dbo].[timeouts](
	[time_to_return] [datetime] NOT NULL,
	[correlation_id] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
	[reply_to] [nvarchar](200) NOT NULL,
	[custom_data] [nvarchar](MAX) NULL,
 CONSTRAINT [PK_timeouts] PRIMARY KEY CLUSTERED 
(
	[time_to_return] ASC,
	[correlation_id] ASC,
	[reply_to] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO