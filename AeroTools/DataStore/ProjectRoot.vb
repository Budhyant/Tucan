﻿'Open VOGEL (www.openvogel.com)
'Open source software for aerodynamics
'Copyright (C) 2016 Guillermo Hazebrouck (guillermo.hazebrouck@openvogel.com)

'This program Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'This program Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty Of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with this program.  If Not, see < http:  //www.gnu.org/licenses/>.

Imports SharpGL
Imports MathTools.Algebra.EuclideanSpace
Imports AeroTools.VisualModel.Models
Imports AeroTools.CalculationModel.Solver
Imports AeroTools.CalculationModel.Settings
Imports AeroTools.CalculationModel.SimulationTools
Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Xml
Imports AeroTools.VisualModel.Tools.Properties
Imports AeroTools.VisualModel.Interface

Namespace DataStore

    Public Enum InterfaceModes As Integer
        Design = 1
        Postprocess = 2
    End Enum

    Public Module ProjectRoot

        ''' <summary>
        ''' Project name
        ''' </summary>
        ''' <returns></returns>
        Public Property Name As String = "New aircraft"

        Public Property FilePath As String = ""
        Public Property SimulationSettings As New SimulationSettings
        Public Property Model As DesignModel
        Public Property Results As New ResultModel
        Public Property VelocityPlane As New VelocityPlane
        Public Property CalculationCore As Solver

        Private _Initialized As Boolean = False

        ReadOnly Property Initialized As Boolean
            Get
                Return _Initialized
            End Get
        End Property

        Public Sub Initialize()

            ControlGL = New OpenGL

            Model = New VisualModel.Models.DesignModel

            SimulationSettings.InitializaParameters()

            VelocityPlane.GenerarMallado()

            Visualization.Initialize()

            Visualization.Proximity = 100

            Results.InitializeResults()

            RefreshOnGL()

            AddHandler Model.OperationsTool.OnTaskReady, AddressOf RefreshOnGL

            _Initialized = True

        End Sub

        Public Sub RestartProject()

            Name = "New aircraft"
            Model.Objects.Clear()
            SimulationSettings.InitializaParameters()
            Visualization.Initialize()
            Results.InitializeResults()
            RefreshOnGL()

        End Sub

        Private _InterfaceMode As InterfaceModes = InterfaceModes.Design

        Public ReadOnly Property InterfaceMode As InterfaceModes
            Get
                Return _InterfaceMode
            End Get
        End Property

        Public ReadOnly Property CurrentModeName As String
            Get
                Select Case _InterfaceMode
                    Case InterfaceModes.Design
                        Return "Design"
                    Case InterfaceModes.Postprocess
                        Return "Postprocess"
                    Case Else
                        Return "No mode"
                End Select
            End Get
        End Property

        Public Sub DesignMode()

            _InterfaceMode = InterfaceModes.Design

            RefreshOnGL()

        End Sub

        Public Sub PostprocessMode()
            If Results.Loaded Then
                _InterfaceMode = InterfaceModes.Postprocess
                RefreshOnGL()
            Else
                'MsgBox("Unable to load post process. No results available.", MsgBoxStyle.Exclamation)
            End If

        End Sub

        Public Sub ShowModeWarning()
            If _InterfaceMode = InterfaceModes.Postprocess Then
                MsgBox("This function is only available in design mode", MsgBoxStyle.Information, "VOGEL")
            ElseIf _InterfaceMode = InterfaceModes.Design Then
                MsgBox("This function is only available in post process mode", MsgBoxStyle.Information, "VOGEL")
            End If
        End Sub

        Public ReadOnly Property ExistsOnDatabase As Boolean
            Get
                Return System.IO.File.Exists(FilePath)
            End Get
        End Property

#Region " Visualization and selection with SharpGL "

        Public Property Visualization As New VisualizationParameters

        Private ControlGL As OpenGL

        Public Property ControlGLWidth As Integer

        Public Property ControlGLHeight As Integer

        ''' <summary>
        ''' SharpGL control used to draw this project.
        ''' </summary>
        ''' <remarks></remarks>
        Private Structure GLElement

            Public Name As UInteger
            Public ShowOnPan As Boolean
            Public ShowOnRotate As Boolean

        End Structure

        ''' <summary>
        ''' Stablishes a reference to an SharpGL control.
        ''' </summary>
        ''' <param name="Control"></param>
        ''' <remarks></remarks>
        Public Sub SetControlGL(ByRef Control As OpenGL)
            ControlGL = Control
        End Sub

        ''' <summary>
        ''' Represents the current lists on OpenGL.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RepresentOnGL()

            ' Don't do it if there is a simulation on course:

            If Simulating Then Return

            'FastRefreshOnGL()

            ControlGL.ClearColor(Visualization.ScreenColor.R / 255, Visualization.ScreenColor.G / 255, Visualization.ScreenColor.B / 255, Visualization.ScreenColor.A / 255)

            ControlGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT)
            ControlGL.Clear(OpenGL.GL_DEPTH_BUFFER_BIT)

            If Visualization.AllowAlphaBlending Then
                ControlGL.Enable(OpenGL.GL_BLEND)
                ControlGL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA)
            Else
                ControlGL.Disable(OpenGL.GL_BLEND)
            End If

            If Visualization.AllowLineSmoothing Then ControlGL.Enable(OpenGL.GL_LINE_SMOOTH) Else ControlGL.Disable(OpenGL.GL_LINE_SMOOTH)

            Dim Origen As New EVector3
            Dim Punto As EVector3 = Visualization.CameraPosition
            Dim Orientacion As EulerAngles = Visualization.CameraOrientation

            ControlGL.RenderMode(OpenGL.GL_RENDER)
            ControlGL.MatrixMode(OpenGL.GL_PROJECTION)

            ControlGL.LoadIdentity()

            ControlGL.Ortho(-0.5 * ControlGLWidth, 0.5 * ControlGLWidth, -0.5 * ControlGLHeight, 0.5 * ControlGLHeight, -100000, 100000)

            ControlGL.Translate(Visualization.CameraPosition.X, Visualization.CameraPosition.Y, 0)
            ControlGL.Rotate(Orientacion.Fi, Orientacion.Tita, Orientacion.Psi)
            ControlGL.Scale(Visualization.Proximity, Visualization.Proximity, Visualization.Proximity)

            For Each List In ListOfSurfacesToDraw
                If Not Visualization.Panning And Not Visualization.Rotating Or
                   (Visualization.Panning And List.ShowOnPan) Or
                   (Visualization.Rotating And List.ShowOnRotate) Then
                    ControlGL.CallList(List.Name)
                End If
            Next

            RepresentTaskOnGL()

            ControlGL.Flush()

        End Sub

        ''' <summary>
        ''' Remakes OpenGL lists and redraws.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RefreshOnGL()

            If IsNothing(ControlGL) Or Simulating Then Return

            'FastRefreshOnGL()

            MakeDrawingLists()
            RepresentOnGL()

        End Sub

        ''' <summary>
        ''' Redraws by using drawing subs 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub FastRefreshOnGL(Width As Integer, Height As Integer)

            ControlGL.ClearColor(Visualization.ScreenColor.R / 255, Visualization.ScreenColor.G / 255, Visualization.ScreenColor.B / 255, Visualization.ScreenColor.A / 255)

            ControlGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT)
            ControlGL.Clear(OpenGL.GL_DEPTH_BUFFER_BIT)

            If Visualization.AllowAlphaBlending Then
                ControlGL.Enable(OpenGL.GL_BLEND)
                ControlGL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA)
            Else
                ControlGL.Disable(OpenGL.GL_BLEND)
            End If

            If Visualization.AllowLineSmoothing Then ControlGL.Enable(OpenGL.GL_LINE_SMOOTH) Else ControlGL.Disable(OpenGL.GL_LINE_SMOOTH)

            Dim Origen As New EVector3
            Dim Punto As EVector3 = Visualization.CameraPosition
            Dim Orientacion As EulerAngles = Visualization.CameraOrientation

            ControlGL.RenderMode(OpenGL.GL_RENDER)
            ControlGL.MatrixMode(OpenGL.GL_PROJECTION)

            ControlGL.LoadIdentity()

            ControlGL.Ortho(-0.5 * Width, 0.5 * Width, -0.5 * Height, 0.5 * Height, -100000, 100000)

            ControlGL.Translate(Visualization.CameraPosition.X, Visualization.CameraPosition.Y, 0)
            ControlGL.Rotate(Orientacion.Fi, Orientacion.Tita, Orientacion.Psi)
            ControlGL.Scale(Visualization.Proximity, Visualization.Proximity, Visualization.Proximity)

            Visualization.Axes.Extension = SimulationSettings.CharacteristicLenght
            Visualization.Axes.GenerateWireFrame(ControlGL)
            Visualization.ReferenceFrame.GenerateWireFrame(ControlGL)

            'Simulacion.RepresentarVectorDeVelocidad(GL, New EVector3)

            ' Model:
            Select Case InterfaceMode

                Case InterfaceModes.Design

                    For i = 0 To Model.Objects.Count - 1

                        If Model.Objects(i).VisualProperties.ShowSurface Then

                            Model.Objects(i).Refresh3DModel(ControlGL)

                        End If

                    Next

                Case InterfaceModes.Postprocess

                    ' Results:

                    If Results.VisualizeModes Then

                        Results.DynamicModes(Results.SelectedModeIndex).Refresh3DModel(ControlGL)

                    Else

                        If Results.Model.VisualProperties.ShowSurface Then

                            Results.Model.Refresh3DModel(ControlGL)

                        End If

                        If Not IsNothing(Results.Wakes) Then

                            Results.Wakes.Refresh3DModel(ControlGL)

                        End If

                        VelocityPlane.ActualizarModelo3D(ControlGL)

                    End If

            End Select

            RepresentTaskOnGL()

            ControlGL.Flush()

        End Sub

        ''' <summary>
        ''' OpenGL lists to be drawn.
        ''' </summary>
        ''' <remarks></remarks>
        Private ListOfSurfacesToDraw As New List(Of GLElement)

        ''' <summary>
        ''' Constructs OpenGL lists.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MakeDrawingLists()

            For Each List In ListOfSurfacesToDraw
                ControlGL.DeleteLists(List.Name, 1)
            Next

            ListOfSurfacesToDraw.Clear()

            Visualization.Axes.Extension = SimulationSettings.CharacteristicLenght

            Dim Coordinates As GLElement
            Coordinates.Name = ControlGL.GenLists(1)
            Coordinates.ShowOnPan = True
            Coordinates.ShowOnRotate = True
            ListOfSurfacesToDraw.Add(Coordinates)
            ControlGL.NewList(Coordinates.Name, OpenGL.GL_COMPILE)
            Visualization.Axes.GenerateWireFrame(ControlGL)
            ControlGL.EndList()

            Dim Reference As GLElement
            Reference.Name = ControlGL.GenLists(1)
            Reference.ShowOnPan = True
            Reference.ShowOnRotate = True
            ListOfSurfacesToDraw.Add(Reference)
            ControlGL.NewList(Reference.Name, OpenGL.GL_COMPILE)
            Visualization.ReferenceFrame.GenerateWireFrame(ControlGL)
            ControlGL.EndList()

            'Simulacion.RepresentarVectorDeVelocidad(GL, New EVector3)

            ' Model:
            Select Case InterfaceMode

                Case InterfaceModes.Design

                    For Each Surface In Model.Objects

                        If Surface.VisualProperties.ShowSurface Then

                            Dim List As GLElement
                            List.Name = ControlGL.GenLists(1)
                            List.ShowOnPan = True
                            List.ShowOnRotate = True
                            ListOfSurfacesToDraw.Add(List)
                            ControlGL.NewList(List.Name, OpenGL.GL_COMPILE)
                            Surface.Refresh3DModel(ControlGL)
                            ControlGL.EndList()

                        End If

                    Next

                Case InterfaceModes.Postprocess

                    If Results.VisualizeModes And Not IsNothing(Results.SelectedMode) Then
                        Dim List As GLElement
                        List.Name = ControlGL.GenLists(1)
                        List.ShowOnPan = True
                        List.ShowOnRotate = True
                        ListOfSurfacesToDraw.Add(List)
                        ControlGL.NewList(List.Name, OpenGL.GL_COMPILE)
                        Results.SelectedMode.Refresh3DModel(ControlGL)
                        ControlGL.EndList()
                    Else

                        ' Results:

                        If Results.Model.VisualProperties.ShowSurface Then
                            Dim List As GLElement
                            List.Name = ControlGL.GenLists(1)
                            List.ShowOnPan = True
                            List.ShowOnRotate = True
                            ListOfSurfacesToDraw.Add(List)
                            ControlGL.NewList(List.Name, OpenGL.GL_COMPILE)
                            Results.Model.Refresh3DModel(ControlGL)
                            ControlGL.EndList()
                        End If

                        If Not IsNothing(Results.Wakes) Then
                            Dim List As GLElement
                            List.Name = ControlGL.GenLists(1)
                            List.ShowOnPan = False
                            List.ShowOnRotate = False
                            ListOfSurfacesToDraw.Add(List)
                            ControlGL.NewList(List.Name, OpenGL.GL_COMPILE)
                            Results.Wakes.Refresh3DModel(ControlGL)
                            ControlGL.EndList()
                        End If

                        Dim Plane As GLElement
                        Plane.Name = ControlGL.GenLists(1)
                        Plane.ShowOnPan = False
                        Plane.ShowOnRotate = False
                        ListOfSurfacesToDraw.Add(Plane)
                        ControlGL.NewList(Plane.Name, OpenGL.GL_COMPILE)
                        VelocityPlane.ActualizarModelo3D(ControlGL)
                        ControlGL.EndList()

                    End If

            End Select

        End Sub

        Public ReadOnly Property ResultsFilePath As String
            Get
                If FilePath <> "" Then
                    Try
                        Return Left(FilePath, FilePath.Length - 4) & ".res"
                    Catch ex As Exception
                        MsgBox("Cannot find resuts file path.", MsgBoxStyle.Exclamation)
                        Return ""
                    End Try
                Else
                    Return ""
                End If
            End Get
        End Property

        'Private Sub RepresentStaticResultsWithOpenGL()

        '    'Dim GL As OpenGL = ControlGL.OpenGL

        '    'GL.ClearColor(VisualizationParameters.ColorDePantalla.R / 255, VisualizationParameters.ColorDePantalla.G / 255, VisualizationParameters.ColorDePantalla.B / 255, VisualizationParameters.ColorDePantalla.A / 255)

        '    'GL.Clear(OpenGL.GL_COLOR_BUFFER_BIT)
        '    'GL.Clear(OpenGL.GL_DEPTH_BUFFER_BIT)

        '    'If VisualizationParameters.PermitirAlphaBlending Then
        '    '    GL.Enable(OpenGL.GL_BLEND)
        '    '    GL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA)
        '    'Else
        '    '    GL.Disable(OpenGL.GL_BLEND)
        '    'End If

        '    'If VisualizationParameters.SuavisadoDeLineas Then GL.Enable(OpenGL.GL_LINE_SMOOTH) Else GL.Disable(OpenGL.GL_LINE_SMOOTH)

        '    'Dim Origen As New EPoint3
        '    'Dim Punto As EPoint3 = VisualizationParameters.PoscionDeCamara
        '    'Dim Orientacion As TOrientacion = VisualizationParameters.OrientacionDeCamara

        '    'GL.RenderMode(OpenGL.GL_RENDER)
        '    'GL.MatrixMode(OpenGL.GL_PROJECTION)

        '    'GL.LoadIdentity()

        '    'GL.Ortho(-0.5 * ControlGL.Width, 0.5 * ControlGL.Width, -0.5 * ControlGL.Height, 0.5 * ControlGL.Height, -100000, 100000)

        '    'GL.Translate(VisualizationParameters.PoscionDeCamara.X, VisualizationParameters.PoscionDeCamara.Y, 0)
        '    'GL.Rotate(Orientacion.Fi, Orientacion.Tita, Orientacion.Psi)
        '    'GL.Scale(VisualizationParameters.Acercamiento, VisualizationParameters.Acercamiento, VisualizationParameters.Acercamiento)

        '    'VisualizationParameters.EjeDeCoordenadas.Extension = Simulacion.LongitudCaracterística
        '    'VisualizationParameters.EjeDeCoordenadas.CrearWireFrame(GL)
        '    'VisualizationParameters.ReferenceFrame.CrearWireFrame(GL)

        '    'Simulacion.RepresentarVectorDeVelocidad(GL, Origen)

        '    'If Resultados.ObtenerModelo.VProps.MostrarSuperficie Then
        '    '    Resultados.ObtenerModelo.ActualizarModelo3D(GL)
        '    'End If

        '    'If Not Me.VisualizationParameters.Panning And Not Me.VisualizationParameters.Rotating Then
        '    '    If Not IsNothing(Resultados.ObtenerEstelas) Then
        '    '        Resultados.ObtenerEstelas.ActualizarModelo3D(GL)
        '    '    End If
        '    'End If

        '    'PlanoDeVelocidad.ActualizarModelo3D(GL)

        '    'GL.Flush()

        'End Sub

        Public Simulating As Boolean = False

        Public Sub RepresentResultsTransitWithOpenGL(ByVal TimeStep As Integer)

            ControlGL.ClearColor(Visualization.ScreenColor.R / 255, Visualization.ScreenColor.G / 255, Visualization.ScreenColor.B / 255, Visualization.ScreenColor.A / 255)

            ControlGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT)
            ControlGL.Clear(OpenGL.GL_DEPTH_BUFFER_BIT)

            If Visualization.AllowAlphaBlending Then
                ControlGL.Enable(OpenGL.GL_BLEND)
                ControlGL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA)
            Else
                ControlGL.Disable(OpenGL.GL_BLEND)
            End If

            If Visualization.AllowLineSmoothing Then ControlGL.Enable(OpenGL.GL_LINE_SMOOTH) Else ControlGL.Disable(OpenGL.GL_LINE_SMOOTH)

            Dim Origen As New EVector3
            Dim Punto As EVector3 = Visualization.CameraPosition
            Dim Orientacion As EulerAngles = Visualization.CameraOrientation

            ControlGL.RenderMode(OpenGL.GL_RENDER)
            ControlGL.MatrixMode(OpenGL.GL_PROJECTION)

            ControlGL.LoadIdentity()

            ControlGL.Ortho(-0.5 * ControlGLWidth, 0.5 * ControlGLWidth, -0.5 * ControlGLHeight, 0.5 * ControlGLHeight, -100000, 100000)

            ControlGL.Translate(Visualization.CameraPosition.X, Visualization.CameraPosition.Y, 0)
            ControlGL.Rotate(Orientacion.Fi, Orientacion.Tita, Orientacion.Psi)
            ControlGL.Scale(Visualization.Proximity, Visualization.Proximity, Visualization.Proximity)

            Visualization.Axes.Extension = SimulationSettings.CharacteristicLenght
            Visualization.Axes.GenerateWireFrame(ControlGL)
            Visualization.ReferenceFrame.GenerateWireFrame(ControlGL)

            SimulationSettings.RepresentVelocityVector(ControlGL, Origen)

            Results.RepresentTransitState(ControlGL, TimeStep)

            ControlGL.Flush()

        End Sub

        Private Sub SelectElementAtDesignWithOpenGL(ByVal X As Double, ByVal Y As Double)

            ControlGL.ClearColor(Visualization.ScreenColor.R / 255, Visualization.ScreenColor.G / 255, Visualization.ScreenColor.B / 255, Visualization.ScreenColor.A / 255)

            Dim Buffer(OpenGL.GL_SELECTION_BUFFER_SIZE) As UInteger
            ControlGL.SelectBuffer(512, Buffer)

            ControlGL.RenderMode(OpenGL.GL_SELECT)
            ControlGL.InitNames()

            ControlGL.LoadIdentity()

            Dim Viewport(3) As Integer
            ControlGL.GetInteger(OpenGL.GL_VIEWPORT, Viewport)
            ControlGL.PickMatrix(X, Viewport(3) - Y, 15, 15, Viewport)

            ControlGL.Ortho(-0.5 * ControlGLWidth, 0.5 * ControlGLWidth, -0.5 * ControlGLHeight, 0.5 * ControlGLHeight, -100000, 100000)

            Dim Origen As New EVector3
            Dim Punto As EVector3 = Visualization.CameraPosition
            Dim Orientacion As EulerAngles = Visualization.CameraOrientation

            ControlGL.Translate(Visualization.CameraPosition.X, Visualization.CameraPosition.Y, 0)
            ControlGL.Rotate(Orientacion.Fi, Orientacion.Tita, Orientacion.Psi)
            ControlGL.Scale(Visualization.Proximity, Visualization.Proximity, Visualization.Proximity)

            'VisualizationParameters.EjeDeCoordenadas.Extension = SimulationSettings.CharacteristicLenght
            'VisualizationParameters.EjeDeCoordenadas.CrearWireFrame(GL)
            'VisualizationParameters.ReferenceFrame.CrearWireFrame(GL)

            SimulationSettings.RepresentVelocityVector(ControlGL, Origen)

            For i = 0 To Model.Objects.Count - 1

                If Model.Objects(i) IsNot Nothing Then

                    Model.Objects(i).Selected = False ' unselect the surface

                    If Not Model.Selection.MultipleSelection Then Model.Objects(i).UnselectAll()

                    If Model.Objects(i).VisualProperties.ShowSurface Then

                        Model.Objects(i).Refresh3DModel(ControlGL, Model.Selection.SelectionMode, i)

                    End If

                End If

            Next

            ControlGL.Flush()

            Dim Hits As Integer = ControlGL.RenderMode(OpenGL.GL_RENDER)

            ' Read the buffer and store selected elements on selection list:

            If Not Model.Selection.MultipleSelection Then Model.Selection.SelectionList.Clear()

            If Hits > 0 Then

                Dim InitialPos As Integer = 0
                Dim PreviousHits As Integer = 0
                Dim SelectedItem As SelectionRecord
                Dim AddNode As Boolean = True
                Dim AddVortex As Boolean = True
                Dim AddRing As Boolean = True
                Dim AddStrElement As Boolean = True
                Dim AddStrNode As Boolean = True

                For i = 1 To Hits

                    PreviousHits = Buffer(InitialPos)

                    If PreviousHits > 0 Then

                        SelectedItem.ID = Buffer(InitialPos + 2 + PreviousHits)

                        ' It will only select one element of each entity type. This is to avoid confusion as geometric actions are excecuted.

                        Model.Objects(SelectedItem.ComponentIndex).Selected = True

                        Select Case SelectedItem.EntityType

                            Case EntityTypes.etNode

                                If AddNode Then Model.Selection.SelectionList.Add(SelectedItem)
                                AddNode = False

                            Case EntityTypes.etVortex
                                If AddVortex Then Model.Selection.SelectionList.Add(SelectedItem)
                                AddVortex = False

                            Case EntityTypes.etQuadPanel
                                If AddRing Then Model.Selection.SelectionList.Add(SelectedItem)
                                AddRing = False

                            Case EntityTypes.etStructuralElement
                                If AddStrElement Then Model.Selection.SelectionList.Add(SelectedItem)
                                AddStrElement = False

                            Case EntityTypes.etStructuralNode
                                If AddStrNode Then Model.Selection.SelectionList.Add(SelectedItem)
                                AddStrNode = False

                            Case Else

                                ' Other elements are loaded

                                Model.Selection.SelectionList.Add(SelectedItem)

                        End Select

                    End If

                    InitialPos = InitialPos + 3 + PreviousHits

                Next

            End If

        End Sub

        Public Sub SelectOnGL(ByVal X As Double, ByVal Y As Double, Width As Integer, Height As Integer)

            Select Case InterfaceMode

                Case InterfaceModes.Design

                    SelectElementAtDesignWithOpenGL(X, Y)

            End Select

            RefreshOnGL()

        End Sub

        Public Sub RepresentTaskOnGL()

            Model.OperationsTool.RepresentTaskOnGL(ControlGL)

        End Sub

#End Region

#Region " Calculation "

        Private CalculationForm As FormProgress

        Private Sub StartWakeConvection(ByVal sender As Object, ByVal e As DoWorkEventArgs)

            CalculationCore.SteadyState(FilePath)

        End Sub

        Private Sub StartUnsteadyTransit(ByVal sender As Object, ByVal e As DoWorkEventArgs)

            CalculationCore.UnsteadyTransit(FilePath)

        End Sub

        Private Sub StartAeroelsaticTransit(ByVal sender As Object, ByVal e As DoWorkEventArgs)

            CalculationCore.AeroelasticUnsteadyTransit(FilePath)

        End Sub

        Private CalculationWorker As BackgroundWorker

        Public Sub StartCalculation(ByVal Type As CalculationType, ByRef Parent As Control)

            If Not IsNothing(CalculationWorker) Then
                If CalculationWorker.IsBusy Then
                    Return
                Else
                    CalculationWorker.Dispose()
                End If
            End If

            CalculationForm = New FormProgress
            If Not IsNothing(Parent) Then CalculationForm.Owner = Parent
            CalculationForm.ClearMessages()
            CalculationForm.PushState("Calculating...")
            CalculationForm.Show()
            CalculationForm.PushMessage("Preparing calculation cell")
            SimulationSettings.AnalysisType = Type

            Try

                CalculationCore = New Solver(Model, SimulationSettings, Type = CalculationType.ctAeroelastic)

                AddHandler CalculationCore.PushProgress, AddressOf CalculationForm.PushMessageWithProgress
                AddHandler CalculationCore.PushMessage, AddressOf CalculationForm.PushMessage
                AddHandler CalculationCore.CalculationDone, AddressOf CalculationFinished
                AddHandler CalculationCore.CalculationDone, AddressOf CalculationForm.ChangeToCloseModus
                AddHandler CalculationCore.CalculationAborted, AddressOf CalculationAborted

                Dim StartingTime As Date = Now
                Results.SimulationSettings = CalculationCore.Settings
                CalculationForm.PushMessage("Calculating with parallel solver")

                CalculationWorker = New BackgroundWorker
                CalculationWorker.WorkerSupportsCancellation = True
                CalculationWorker.WorkerReportsProgress = True
                AddHandler CalculationForm.CancellationRequested, AddressOf CalculationCore.RequestCancellation

                Select Case Type

                    Case CalculationType.ctSteady

                        AddHandler CalculationWorker.DoWork, AddressOf StartWakeConvection

                    Case CalculationType.ctUnsteady

                        AddHandler CalculationWorker.DoWork, AddressOf StartUnsteadyTransit

                    Case CalculationType.ctAeroelastic

                        AddHandler CalculationWorker.DoWork, AddressOf StartAeroelsaticTransit

                End Select

                CalculationWorker.RunWorkerAsync()

            Catch ex As Exception
                CalculationWorker.CancelAsync()
                CalculationForm.PushMessage(String.Format("Calculation exited with exception: ""{0}"".", ex.Message))
                CalculationForm.PushState("Exited with exception!")
                CalculationForm.ChangeToCloseModus()
                Return
            End Try

        End Sub

        Private Sub CalculationFinished()

            CalculationForm.PushMessage("Loading results")
            CalculationCore.SetCompleteModelOnResults(Results)
            CalculationForm.PushMessage("Ready")
            CalculationForm.PushState("Calculation done")
            RaiseEvent CalculationDone()

        End Sub

        Private Sub CalculationAborted()

            CalculationForm.PushMessage("Calculation aborted")
            CalculationForm.ChangeToCloseModus()
            RaiseEvent CalculationDone()

        End Sub

        ''' <summary>
        ''' Occurs when the calculation finishes.
        ''' </summary>
        ''' <remarks></remarks>
        Public Event CalculationDone()

#End Region

#Region " IO "

        Public Sub ReadFromXML()

            If Not ExistsOnDatabase Then Exit Sub

            Dim reader As XmlReader = XmlReader.Create(FilePath)

            If reader.ReadToDescendant("Project") Then

                Name = reader.GetAttribute("Name")

                While reader.Read

                    Select Case reader.Name

                        Case "Model"
                            Model.ReadFromXML(reader.ReadSubtree)

                        Case "Simulacion"
                            SimulationSettings.ReadFromXML(reader.ReadSubtree)

                        Case "Visualization"
                            Visualization.ReadFromXML(reader.ReadSubtree)

                        Case "VelocityPlane"
                            VelocityPlane.ReadFromXML(reader.ReadSubtree)

                    End Select

                End While

            End If

            reader.Close()

            RepresentOnGL()

        End Sub

        Public Sub WriteToXML()

            Dim writer As XmlWriter = XmlWriter.Create(FilePath)

            writer.WriteStartElement("Project")

            writer.WriteAttributeString("Name", Name)

            writer.WriteStartElement("Model")
            Model.WriteToXML(writer)
            writer.WriteEndElement()

            writer.WriteStartElement("Simulacion")
            SimulationSettings.SaveToXML(writer)
            writer.WriteEndElement()

            writer.WriteStartElement("Visualization")
            Visualization.SaveToXML(writer)
            writer.WriteEndElement()

            writer.WriteStartElement("VelocityPlane")
            VelocityPlane.SaveToXML(writer)
            writer.WriteEndElement()

            writer.WriteEndElement() ' Project

            writer.Close()

        End Sub

        Public Sub ReadResults(ByVal FilePath As String)

            CalculationCore = New Solver()
            CalculationCore.ReadFromXML(FilePath)
            CalculationCore.SetCompleteModelOnResults(Results)

        End Sub

#End Region

    End Module

End Namespace






