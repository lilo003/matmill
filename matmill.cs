﻿using System;

using CamBam.CAD;
using CamBam.Geom;

namespace Matmill
{

    class Pocket_generator
    {
        private const double TED_TOLERANCE_PERCENTAGE = 0.001;  // 0.1 %
        private const double FINAL_ALLOWED_TED_OVERSHOOT_PERCENTAGE = 0.03;  // 3 %

        private readonly Topographer _topo;

        private double _general_tolerance = 0.001;
        private double _tool_r = 1.5;
        private double _margin = 0;
        private double _max_ted = 3.0 * 0.4;
        private double _min_ted = 3.0 * 0.1;
        private Point2F _startpoint = Point2F.Undefined;
        private RotationDirection _dir = RotationDirection.CW;
        private bool _should_smooth_chords = false;
        //private bool _should_emit_debug_medial_axis = false;
        private double _slice_leadin_angle = 3 * Math.PI / 180;
        private double _slice_leadout_angle = 0.5 * Math.PI / 180;

        public double Tool_d                                      { set { _tool_r = value / 2.0;}}
        public double General_tolerance                           { set { _general_tolerance = value; } }
        public double Margin                                      { set { _margin = value; } }
        public double Max_ted                                     { set { _max_ted = value; } }
        public double Min_ted                                     { set { _min_ted = value; } }
        public double Slice_leadin_angle                          { set { _slice_leadin_angle = value; } }
        public double Slice_leadout_angle                         { set { _slice_leadout_angle = value; } }
        public Point2F Startpoint                                 { set { _startpoint = value; } }
        public RotationDirection Mill_direction                   { set { _dir = value; } }
        public bool Should_smooth_chords                          { set { _should_smooth_chords = value; }}        

        private double _min_passable_mic_radius
        {
            get { return 0.1 * _tool_r; } // 5 % of tool diameter is seems to be ok
        }

        private double get_mic_radius(Point2F pt)
        {
            return _topo.Get_dist_to_wall(pt) - _tool_r - _margin;
        }

        private Pocket_path generate_path(Slicer slicer)
        {
            Pocket_path_generator gen;

            if (!_should_smooth_chords)
                gen = new Pocket_path_generator(_general_tolerance);
            else
                gen = new Pocket_path_smooth_generator(_general_tolerance, 0.1 * _tool_r);
            
            gen.Append_spiral(slicer.Root_slice.Center, slicer.Root_slice.End, _max_ted, _dir == RotationDirection.Unknown ? RotationDirection.CCW : _dir);
            gen.Append_root_slice(slicer.Root_slice);            

            for (int i = 1; i < slicer.Sequence.Count; i++)
            {
                Slice s = slicer.Sequence[i];
                gen.Append_slice(s, s.Guide);                    
            }
                        
            gen.Append_return_to_base(slicer.Gen_return_path());

            return gen.Path;
        }

        private double radius_getter(Point2F pt)
        {
            double radius = get_mic_radius(pt);
            if (radius < _min_passable_mic_radius) return 0;
            return radius;
        }

        public Pocket_path run()
        {
            if (_dir == RotationDirection.Unknown && _should_smooth_chords)
                throw new Exception("smooth chords are not allowed for the variable mill direction");

            Logger.log("building medial axis");

            Branch tree = new Branch(null);
            bool is_ok = _topo.Build_medial_tree(tree, _tool_r / 10, _general_tolerance, _startpoint, _min_passable_mic_radius + _tool_r + _margin);

            if (! is_ok)
            {
                Logger.warn("failed to build tree");
                return null;
            }            

            Slicer slicer = new Slicer(_topo.Min, _topo.Max, _general_tolerance);                                    
            slicer.Slice_leadin_angle = _slice_leadin_angle;
            slicer.Slice_leadout_angle = _slice_leadout_angle;            
            slicer.Get_radius = radius_getter;

            Logger.log("generating slices");
            slicer.Run(tree, _tool_r, _max_ted, _min_ted, _dir);

            Logger.log("generating path");
            return generate_path(slicer);
        }

        public Pocket_generator(Polyline outline, Polyline[] islands)
        {
            _topo = new Topographer(outline, islands);
        }
    }
}
