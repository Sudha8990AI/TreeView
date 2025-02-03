Public Function GetTreeViewEmpMgr(ByVal si As SessionInfo, ByVal args As DashboardDataSetArgs) As DataSet
	Try
		
		Dim sql As New Text.StringBuilder()
		Dim WFProfileName As String = args.NameValuePairs.XFGetValue("WFProfileName")
		Dim WFScenarioName As String = args.NameValuePairs.XFGetValue("WFScenarioName")
		Dim WFTimeName As String = args.NameValuePairs.XFGetValue("WFTimeName")

		sql.append("Select")
		sql.Append(" CASE WHEN M.RegisterID = E.RegisterID THEN NULL ")
		sql.append(" Else Concat(M.FirstName,' ',M.LastName) End as 'Parent'")
		sql.append(", Concat(E.FirstName,' ',E.LastName) as 'Child'")
		sql.append(" FROM XFW_PLP_Register E ")
		sql.append("LEFT JOIN XFW_PLP_Register M ON E.Code11 = M.RegisterID ")
		sql.append("Where ") 
		sql.Append("E.Status <> 'NewHire' And")
		sql.append("   E.WFScenarioName = '" & WFScenarioName & "' And E.WFTimeName = '" & WFTimeName & "' And E.WFProfileName =  '" & WFProfileName & "' And ")
		sql.append("   (M.WFScenarioName = '" & WFScenarioName & "' And M.WFTimeName = '" & WFTimeName & "' And M.WFProfileName = '" & WFProfileName & "' OR M.RegisterID IS NULL)")
		BRApi.ErrorLog.LogMessage(si,sql.ToString)
		
		Dim dt As DataTable
		'Execute the query
		Using dbConnApp As DbConnInfo = BRApi.Database.CreateApplicationDbConnInfo(si)
			dt = BRApi.Database.ExecuteSql(dbConnApp, sql.ToString(), False)
			dt.TableName = "Employee"
		End Using
		
		' Create the main tree collection.
		Dim treeItems As New XFTreeItemCollection
		
		' Dictionary to keep track of nodes by name.
		Dim nodesByName As New Dictionary(Of String, XFTreeItem)()
		
	
		 ' Common visual properties.
		Dim textColour As String = XFColors.Black.Name
		Dim imageSource As String = XFImageFileSourceType.ClientImage
		Dim imageName As String = XFClientImageTypes.StatusGrayBall.Name
		Dim isBold As Boolean = False
		Dim isEnabled As Boolean = True
		Dim isSelected As Boolean = False
		Dim isExpanded As Boolean = False
		
		#Region "Buid Tree"
		' Process each row from the DataTable.
		Dim i As Integer = 0
		
		For Each row As DataRow In dt.Rows
			
			BRApi.ErrorLog.LogMessage(si,"Loop: " +i.ToString)
			i=i+1
			
			Dim parentName As String = ""
			If Not IsDBNull(row("Parent")) Then
				parentName = row("Parent").ToString().Trim()
				
			End If				    
			Dim childName As String = row("Child").ToString().Trim()

			' Create a node for the child.
			Dim childNode As New XFTreeItem(childName, childName, textColour, isBold, isEnabled, isSelected, isExpanded, imageSource, imageName, childName, Nothing)
			BRApi.ErrorLog.LogMessage(si,"Adding to Child Node: "+childNode.HeaderText)
			
			If String.IsNullOrEmpty(parentName) Then
				' If there is no parent, then this is a root node.
				treeItems.TreeItems.Add(childNode)
				nodesByName(childName) = childNode
				BRApi.ErrorLog.LogMessage(si,"RootNode: "+parentName +" Node: "+nodesByName(childName).HeaderText)
			Else
				' If there is a parent, check if the parent node already exists.
				Dim parentNode As XFTreeItem = Nothing
				If nodesByName.ContainsKey(parentName) Then
					parentNode = nodesByName(parentName)
					BRApi.ErrorLog.LogMessage(si,"Parent is there: "+parentNode.HeaderText)
				Else
					' Parent does not exist yet; create it as a root node.
					parentNode = New XFTreeItem(parentName, parentName, textColour, isBold, isEnabled, isSelected, True, imageSource, imageName, parentName,Nothing)
					BRApi.ErrorLog.LogMessage(si,"Created New Parent Node:"+parentNode.HeaderText)
					
					treeItems.TreeItems.Add(parentNode)
					nodesByName(parentName) = parentNode
					
				End If

				' Add the child node to the parent's children collection.
				If parentNode.Children Is Nothing Then
						parentNode.IsBold = True
						parentNode.IsExpanded =True
						parentNode.Children = New List(Of XFTreeItem)
						
				End If
				parentNode.HeaderText = parentName+ " ("+(parentNode.Children.Count+1).ToString+")"
				parentNode.Children.Add(childNode)
				
				BRApi.ErrorLog.LogMessage(si,"Adding to Child Node to Parent Node: "+parentNode.HeaderText +"  ---> "+ childNode.HeaderText)
				
				' Also add the child node to the dictionary.
				If Not nodesByName.ContainsKey(childName) Then
					nodesByName.Add(childName, childNode)
				End If
				
			End If
			
			'BRApi.ErrorLog.LogMessage(si,childName)
			
		
		Next
		
		#End Region
	
	
		Return treeItems.CreateDataSet(si)
		
	Catch ex As Exception
		Throw ErrorHandler.LogWrite(si, New XFException(si, ex))
	End Try
End Function
