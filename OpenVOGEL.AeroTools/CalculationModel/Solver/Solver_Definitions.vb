﻿'Open VOGEL (https://en.wikibooks.org/wiki/Open_VOGEL)
'Open source software for aerodynamics
'Copyright (C) 2018 Guillermo Hazebrouck (gahazebrouck@gmail.com)

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

Imports OpenVOGEL.AeroTools.CalculationModel.Settings
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Structural
Imports OpenVOGEL.MathTools.Algebra.EuclideanSpace
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Aero
Imports DotNumerics.LinearAlgebra
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Aero.Components

Namespace CalculationModel.Solver

    Partial Public Class Solver

        Public Sub New()
            Lattices = New List(Of BoundedLattice)
            Settings = New SimulationSettings
        End Sub

        ' Public fields:

        ''' <summary>
        ''' Contains all parameters required to run a complete simulation
        ''' </summary>
        Public Property Settings As New SimulationSettings

        ''' <summary>
        ''' Contains all bounded lattices
        ''' </summary>
        ''' <remarks></remarks>
        Public Property Lattices As List(Of BoundedLattice)

        ''' <summary>
        ''' Stores links between the structure and the aerodynamic lattices
        ''' </summary>
        ''' <remarks></remarks>
        Public Property StructuralLinks As List(Of StructuralLink)

        ''' <summary>
        ''' Gathers all polar curves necessary for the calculation.
        ''' </summary>
        ''' <remarks></remarks>
        Public Property PolarDataBase As PolarDatabase

        ' Private calculation variables

        Private MatrixDoublets As Matrix
        Private MatrixSources As Matrix
        Private G As Vector
        Private S As Vector
        Private RHS As Vector
        Private Dimension As Integer

        Private _StreamVelocity As New Vector3
        Private _StreamDensity As Double
        Private _StreamDynamicPressure As Double
        Private _StreamOmega As New Vector3

        ' Public properties:

        ''' <summary>
        ''' Base stream velocity in m/s
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property StreamVelocity As Vector3
            Get
                Return _StreamVelocity
            End Get
        End Property

        ''' <summary>
        ''' Stream density in kg/m³
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property StreamDensity As Double
            Get
                Return _StreamDensity
            End Get
        End Property

        ''' <summary>
        ''' Stream density in Pa
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property StreamDynamicPressure As Double
            Get
                Return _StreamDynamicPressure
            End Get
        End Property

        ''' <summary>
        ''' Occurs when a progress is made.
        ''' </summary>
        ''' <param name="Title"></param>
        ''' <param name="Value"></param>
        ''' <remarks></remarks>
        Public Event PushProgress(ByVal Title As String, ByVal Value As Integer)

        ''' <summary>
        ''' Occurs when a progress is made.
        ''' </summary>
        ''' <param name="Title"></param>
        ''' <remarks></remarks>
        Public Event PushMessage(ByVal Title As String)

        ''' <summary>
        ''' Occurs when the calculation finishes.
        ''' </summary>
        ''' <remarks></remarks>
        Public Event CalculationDone()

        ''' <summary>
        ''' Occurs when the calculation is automatically aborted.
        ''' </summary>
        Public Event CalculationAborted()

    End Class

End Namespace