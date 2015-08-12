USE [rebus_test]
GO

CREATE TABLE [dbo].[subscriptions](
	[message_type] [nvarchar](200) NOT NULL,
	[endpoint] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_subscriptions] PRIMARY KEY CLUSTERED 
(
	[message_type] ASC,
	[endpoint] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

 
